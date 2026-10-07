using System.Collections.Concurrent;
using ProjectHub.Core.Infrastructure;
using ProjectHub.Core.Models;
using ProjectHub.Core.Utilities;
namespace ProjectHub.Core.Services.Monitoring;

public sealed class ProjectWatcher(HubLog log) : IDisposable
{
    private readonly List<FileSystemWatcher> watchers = [];
    private readonly ConcurrentDictionary<string, DateTimeOffset> pending = new(StringComparer.OrdinalIgnoreCase);
    private Timer? timer;
    private HubSettings? settings;
    public event Action<string[]>? Changed;
    public event Action? Overflow;
    public int Count => watchers.Count;
    public void Start(HubSettings config)
    {
        Dispose();
        settings = config;
        if (!config.WatchEnabled)
            return;
        foreach (var root in config.Roots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(root))
                continue;
            try
            {
                var w = new FileSystemWatcher(root) { IncludeSubdirectories = true, NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size, InternalBufferSize = 65536 };
                w.Changed += OnChange;
                w.Created += OnChange;
                w.Deleted += OnChange;
                w.Renamed += (s, e) => { Queue(e.OldFullPath); Queue(e.FullPath); };
                w.Error += (s, e) => { log.Write("Watcher", e.GetException().Message); Overflow?.Invoke(); };
                w.EnableRaisingEvents = true;
                watchers.Add(w);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException) { log.Write("Watcher", root + ": " + e.Message); }
        }
        timer = new Timer(_ => Flush(), null, 1500, 1500);
    }
    private void OnChange(object sender, FileSystemEventArgs e) => Queue(e.FullPath);
    public void Defer(IEnumerable<string> paths)
    {
        foreach (var path in paths)
            Queue(path);
    }
    private void Queue(string path)
    {
        if (settings == null)
            return;
        var root = settings.Roots.FirstOrDefault(x => SafeFileSystem.Within(path, x));
        if (root == null)
            return;
        var relative = Path.GetRelativePath(root, path);
        var segments = relative.Split(Path.DirectorySeparatorChar);
        // Git refs/index affect repository status. Other generated paths are ignored.
        bool git = segments.Contains(".git", StringComparer.OrdinalIgnoreCase);
        if (!git && segments.Any(x => settings.Exclusions.Contains(x, StringComparer.OrdinalIgnoreCase)))
            return;
        if (pending.Count > 20000)
        {
            pending.Clear();
            Overflow?.Invoke();
            return;
        }
        pending[path] = DateTimeOffset.UtcNow;
    }
    private void Flush()
    {
        var ready = pending.Where(x => DateTimeOffset.UtcNow - x.Value > TimeSpan.FromSeconds(2)).Select(x => x.Key).ToArray();
        foreach (var p in ready)
            pending.TryRemove(p, out _);
        if (ready.Length > 0)
            Changed?.Invoke(ready);
    }
    public void Dispose()
    {
        timer?.Dispose();
        timer = null;
        foreach (var w in watchers)
            w.Dispose();
        watchers.Clear();
        pending.Clear();
    }
}
