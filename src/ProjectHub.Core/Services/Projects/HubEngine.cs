using ProjectHub.Core.Detection;
using ProjectHub.Core.Infrastructure;
using ProjectHub.Core.Models;
using ProjectHub.Core.Services.Analytics;
using ProjectHub.Core.Services.Git;
using ProjectHub.Core.Services.Monitoring;
using ProjectHub.Core.Services.Persistence;
using ProjectHub.Core.Services.Scanning;
using ProjectHub.Core.Utilities;
namespace ProjectHub.Core.Services.Projects;

public sealed class HubEngine : IDisposable
{
    public HubRepository Repository
    {
        get;
    }
    public HubLog Log
    {
        get;
    }
    public HubSettings Settings { get; private set; } = new();
    private readonly List<ProjectRecord> projects = [];
    private readonly object projectGate = new();
    public List<ProjectRecord> Projects
    {
        get
        {
            lock (projectGate)
                return projects.ToList();
        }
    }
    public List<string> ScanErrors { get; private set; } = [];
    public ProjectWatcher Watcher
    {
        get;
    }
    public event Action? Updated;
    private readonly ProjectScanner scanner = new(new DetectorRegistry());
    private readonly ProjectEnricher enricher = new(new StatisticsAnalyzer(), new GitService());
    private readonly SemaphoreSlim operations = new(1, 1);
    private readonly SemaphoreSlim relocation = new(1, 1);
    private readonly CancellationTokenSource lifetime = new();
    private Timer? gitTimer;
    public TimeSpan LastScanDuration
    {
        get; private set;
    }
    private int backgroundScanRequested;
    public bool IsBusy => operations.CurrentCount == 0;
    public int ScanQueueCount => backgroundScanRequested;
    public HubEngine(string dataDirectory)
    {
        Repository = new(Path.Combine(dataDirectory, "projecthub.db"));
        Log = new(Path.Combine(dataDirectory, "logs"));
        Watcher = new(Log);
        Watcher.Changed += paths => _ = UpdateChangedAsync(paths);
        Watcher.Overflow += RequestBackgroundScan;
    }
    public async Task InitializeAsync()
    {
        await Repository.InitializeAsync();
        Settings = await Repository.LoadSettingsAsync();
        var cached = await Repository.LoadProjectsAsync();
        lock (projectGate)
            projects.AddRange(cached);
        foreach (var p in Projects)
            p.Missing = !Directory.Exists(p.Path);
        RestartMonitoring();
    }
    public void RestartMonitoring()
    {
        Watcher.Start(Settings);
        gitTimer?.Dispose();
        gitTimer = new Timer(_ => _ = RefreshGitAsync(), null, TimeSpan.FromSeconds(Math.Max(30, Settings.GitRefreshSeconds)), TimeSpan.FromSeconds(Math.Max(30, Settings.GitRefreshSeconds)));
    }
    public async Task ScanAsync(IProgress<ScanProgress>? progress, CancellationToken token)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, lifetime.Token);
        await operations.WaitAsync(linked.Token);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = await scanner.ScanAsync(Settings, progress, linked.Token);
            ScanErrors = result.Errors;
            foreach (var p in Projects)
                p.Missing = !Directory.Exists(p.Path);
            foreach (var error in result.Errors.Take(100))
                Log.Write("Scanning", error);
            var existing = Projects.ToDictionary(x => x.Path, StringComparer.OrdinalIgnoreCase);
            var discovered = new List<ProjectRecord>();
            foreach (var p in result.Projects)
            {
                if (existing.TryGetValue(p.Path, out var old))
                {
                    old.Technologies = p.Technologies;
                    old.Components = p.Components;
                    old.Description = p.Description;
                    old.Missing = false;
                    discovered.Add(old);
                }
                else
                {
                    var customization = Settings.PendingCustomizations.FirstOrDefault(x => x.Path.Equals(p.Path, StringComparison.OrdinalIgnoreCase));
                    if (customization != null)
                    {
                        customization.ApplyTo(p);
                        Settings.PendingCustomizations.Remove(customization);
                    }
                    lock (projectGate)
                        projects.Add(p);
                    discovered.Add(p);
                }
            }
            await Repository.SaveSettingsAsync(Settings);
            var known = Projects;
            foreach (var p in known)
                p.IndependentSubprojects = known.Where(x => x.Id != p.Id && SafeFileSystem.Within(x.Path, p.Path)).Select(x => x.Path).ToList();
            Updated?.Invoke();
            if (!result.Cancelled)
                foreach (var p in Projects.Where(x => result.CompletedRoots.Any(r => SafeFileSystem.Within(x.Path, r))))
                    p.Missing = !Directory.Exists(p.Path);
            // On cancellation, preserve discoveries and all previously cached analysis.
            await Repository.SaveProjectsAsync(Projects);
            if (!result.Cancelled)
            {
                int analyzed = 0;
                await Parallel.ForEachAsync(discovered, new ParallelOptions { MaxDegreeOfParallelism = Math.Clamp(Settings.Concurrency, 1, 8), CancellationToken = linked.Token }, async (p, ct) =>
                {
                    try
                    {
                        await enricher.EnrichAsync(p, Settings, ct);
                        await relocation.WaitAsync(ct);
                        try
                        {
                            var previous = ProjectIdentity.FindMoved(p, Projects);
                            if (previous != null)
                            {
                                ProjectIdentity.PreserveMetadata(previous, p);
                                await Repository.ReplaceMovedAsync(p, previous.Id);
                                lock (projectGate)
                                    projects.Remove(previous);
                            }
                            else
                                await Repository.SaveProjectsAsync([p]);
                        }
                        finally { relocation.Release(); }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException) { Log.Write("Detection", p.Path + ": " + ex.Message); }
                    var count = Interlocked.Increment(ref analyzed);
                    progress?.Report(new(0, discovered.Count, 0, clock.Elapsed, $"Analyse {count}/{discovered.Count} · {p.Name}"));
                    if (count % 5 == 0)
                        Updated?.Invoke();
                });
            }
        }
        catch (OperationCanceledException) { Log.Write("Scanning", "Scan annulé"); }
        finally
        {
            LastScanDuration = clock.Elapsed;
            try
            {
                await Repository.SaveProjectsAsync(Projects);
            }
            finally { operations.Release(); Updated?.Invoke(); }
        }
    }
    private async Task UpdateChangedAsync(string[] paths)
    {
        if (!await operations.WaitAsync(0))
        {
            Watcher.Defer(paths);
            return;
        }
        bool discover = false;
        try
        {
            var changed = Projects.Where(p => paths.Any(x => SafeFileSystem.Within(x, p.Path))).ToArray();
            discover = paths.Any(x => (IsManifest(x) || Directory.Exists(x)) && !Projects.Any(p => SafeFileSystem.Within(x, p.Path)));
            foreach (var p in changed)
            {
                var affected = paths.Where(x => SafeFileSystem.Within(x, p.Path)).ToArray();
                bool full = affected.Any(x => !x.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar));
                if (full)
                {
                    p.LastActivity = DateTimeOffset.UtcNow;
                    ProjectEnricher.AddActivity(p, "Fichiers", $"{affected.Length} chemins modifiés");
                }
                if (full && affected.Any(IsManifest))
                    await RefreshStructureAsync(p, lifetime.Token);
                await enricher.EnrichAsync(p, Settings, lifetime.Token, full);
            }
            await Repository.SaveProjectsAsync(changed);
            Updated?.Invoke();
        }
        catch (Exception e) { if (!lifetime.IsCancellationRequested) Log.Write("Watcher", e.Message); }
        finally { operations.Release(); }
        if (discover)
            RequestBackgroundScan();
    }
    public async Task RefreshProjectAsync(ProjectRecord p, bool full = true)
    {
        await operations.WaitAsync(lifetime.Token);
        try
        {
            if (full)
                await RefreshStructureAsync(p, lifetime.Token);
            await enricher.EnrichAsync(p, Settings, lifetime.Token, full);
            await Repository.SaveProjectsAsync([p]);
            Updated?.Invoke();
        }
        finally { operations.Release(); }
    }
    private async Task RefreshGitAsync()
    {
        if (!await operations.WaitAsync(0))
            return;
        try
        {
            foreach (var p in Projects.Where(x => x.Git.IsRepository).ToArray())
                await enricher.EnrichAsync(p, Settings, lifetime.Token, false);
            await Repository.SaveProjectsAsync(Projects);
            Updated?.Invoke();
        }
        catch (Exception ex) { if (!lifetime.IsCancellationRequested) Log.Write("Git", ex.Message); }
        finally { operations.Release(); }
    }
    public async Task SaveProjectAsync(ProjectRecord p)
    {
        await Repository.SaveProjectsAsync([p]);
        Updated?.Invoke();
    }
    private static bool IsManifest(string path) => new[] { "package.json", "Cargo.toml", "project.godot", "pyproject.toml", "requirements.txt", "build.gradle", "build.gradle.kts", "pom.xml", "CMakeLists.txt", "default.project.json" }.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase) || path.EndsWith("proj", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase);
    private async Task RefreshStructureAsync(ProjectRecord p, CancellationToken token)
    {
        var config = new HubSettings { Roots = [p.Path], Exclusions = Settings.Exclusions, MaxDepth = Settings.MaxDepth, FollowSymlinks = Settings.FollowSymlinks, IncludeHidden = Settings.IncludeHidden };
        var result = await scanner.ScanAsync(config, null, token);
        var root = result.Projects.FirstOrDefault(x => x.Path.Equals(p.Path, StringComparison.OrdinalIgnoreCase));
        if (root != null)
        {
            p.Technologies = root.Technologies;
            p.Components = root.Components;
            p.Description = root.Description;
        }
    }
    private void RequestBackgroundScan()
    {
        if (!Settings.BackgroundScan || lifetime.IsCancellationRequested || Interlocked.Exchange(ref backgroundScanRequested, 1) != 0)
            return;
        _ = Task.Run(async () =>
        {
            try
            {
                await ScanAsync(null, lifetime.Token);
            }
            catch (Exception ex) { if (!lifetime.IsCancellationRequested) Log.Write("Scanning", ex.Message); }
            finally { Interlocked.Exchange(ref backgroundScanRequested, 0); }
        });
    }
    public async Task SaveSettingsAsync()
    {
        await Repository.SaveSettingsAsync(Settings);
        RestartMonitoring();
    }
    public async Task UpdateSettingsAsync(HubSettings settings)
    {
        await Repository.SaveSettingsAsync(settings);
        Settings=settings;
        RestartMonitoring();
    }
    public void Dispose()
    {
        lifetime.Cancel();
        Watcher.Dispose();
        gitTimer?.Dispose();
    }
}
