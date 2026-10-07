using System.Diagnostics;
using ProjectHub.Core.Detection;
using ProjectHub.Core.Models;
using ProjectHub.Core.Utilities;
namespace ProjectHub.Core.Services.Scanning;

public sealed class ProjectScanner(DetectorRegistry registry)
{
    public Task<ScanResult> ScanAsync(HubSettings settings, IProgress<ScanProgress>? progress, CancellationToken token) => Task.Run(() => Scan(settings, progress, token), CancellationToken.None);
    private ScanResult Scan(HubSettings settings, IProgress<ScanProgress>? progress, CancellationToken token)
    {
        var projects = new List<ProjectRecord>();
        var errors = new List<string>();
        var completed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var clock = Stopwatch.StartNew();
        int directories = 0;
        long files = 0;
        long lastReport = 0;
        foreach (var rawRoot in settings.Roots)
        {
            if (token.IsCancellationRequested)
                break;
            string root;
            try
            {
                root = SafeFileSystem.Normalize(rawRoot);
            }
            catch (Exception e) { errors.Add(rawRoot + ": " + e.Message); continue; }
            if (!Directory.Exists(root))
            {
                errors.Add(root + ": dossier indisponible");
                continue;
            }
            var stack = new Stack<(string Path, int Depth, ProjectRecord? Parent)>();
            stack.Push((root, 0, null));
            bool rootFailed = false;
            while (stack.Count > 0 && !token.IsCancellationRequested)
            {
                var (path, depth, parent) = stack.Pop();
                if (!visited.Add(path))
                    continue;
                try
                {
                    // Junctions are resolved when explicitly enabled; a visited target prevents loops.
                    var info = new DirectoryInfo(path);
                    if (info.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        if (!settings.FollowSymlinks)
                            continue;
                        var target = info.ResolveLinkTarget(true);
                        if (target == null || !visited.Add(target.FullName))
                            continue;
                    }
                    directories++;
                    var entries = info.EnumerateFileSystemInfos().Take(100001).ToArray();
                    if (entries.Length > 100000)
                    {
                        errors.Add(path + ": limite de 100 000 entrées atteinte");
                        rootFailed = true;
                    }
                    var filePaths = entries.OfType<FileInfo>().Select(x => x.FullName).ToArray();
                    files += filePaths.Length;
                    bool sample = parent != null && System.IO.Path.GetRelativePath(parent.Path, path).Split(System.IO.Path.DirectorySeparatorChar).Any(x => new[] { "fixtures", "testdata", "tests", "testing", "examples", "samples", "test" }.Contains(x, StringComparer.OrdinalIgnoreCase));
                    if (parent?.Technologies.Contains(".NET") == true && filePaths.Any(x => x.EndsWith(".csproj") || x.EndsWith(".fsproj")))
                        sample = false;
                    var detection = sample ? null : registry.Detect(new(path, filePaths));
                    var current = parent;
                    if (detection != null)
                    {
                        if (parent != null)
                        {
                            parent.Components.Add(new(info.Name, path, detection.Technologies));
                            parent.Technologies = parent.Technologies.Concat(detection.Technologies).Distinct().ToList();
                        }
                        else
                        {
                            var p = new ProjectRecord { Name = info.Name, Path = path, Technologies = detection.Technologies, Description = detection.Description, LastActivity = info.LastWriteTimeUtc };
                            projects.Add(p);
                            current = p;
                        }
                    }
                    // A repository, solution or manifest root owns its nested packages. Unrelated siblings remain separate.
                    if (depth < settings.MaxDepth)
                        foreach (var child in entries.OfType<DirectoryInfo>())
                            if (!SafeFileSystem.Skip(child, settings))
                            {
                                var nestedRepo = Directory.Exists(System.IO.Path.Combine(child.FullName, ".git")) || File.Exists(System.IO.Path.Combine(child.FullName, ".git"));
                                stack.Push((child.FullName, depth + 1, nestedRepo && detection?.OwnsChildren != true ? null : current));
                            }
                    if (clock.ElapsedMilliseconds - lastReport > 100)
                    {
                        progress?.Report(new(directories, projects.Count, files, clock.Elapsed, path));
                        lastReport = clock.ElapsedMilliseconds;
                    }
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException) { errors.Add(path + ": " + e.Message); rootFailed = true; }
            }
            if (!token.IsCancellationRequested && !rootFailed)
                completed.Add(root);
        }
        progress?.Report(new(directories, projects.Count, files, clock.Elapsed, "Scan terminé"));
        return new(projects, errors, completed, token.IsCancellationRequested);
    }
}
