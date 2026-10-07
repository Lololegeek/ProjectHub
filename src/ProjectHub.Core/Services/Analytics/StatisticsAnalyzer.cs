using ProjectHub.Core.Models;
using ProjectHub.Core.Utilities;
namespace ProjectHub.Core.Services.Analytics;

public sealed class StatisticsAnalyzer
{
    private readonly SourceFileAnalyzer source = new();
    private static readonly HashSet<string> Regenerable = new(["node_modules", "target", "bin", "obj", "build", "dist", ".next", ".nuxt", ".cache", ".gradle", ".godot", "__pycache__"], StringComparer.OrdinalIgnoreCase);
    public Task<(ProjectStatistics Stats, DateTimeOffset LastWrite)> AnalyzeAsync(string path, HubSettings settings, CancellationToken token, IReadOnlyCollection<string>? independentRoots = null) => Task.Run(() => AnalyzeAsyncCore(path, settings, token, independentRoots), token);
    private async Task<(ProjectStatistics, DateTimeOffset)> AnalyzeAsyncCore(string root, HubSettings settings, CancellationToken token, IReadOnlyCollection<string>? independentRoots)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var stats = new ProjectStatistics { MeasuredAt = DateTimeOffset.UtcNow };
        var languages = new Dictionary<string, LanguageStatistics>();
        var buckets = new Dictionary<string, (string Path, long Bytes, bool Regenerable)>();
        var stack = new Stack<(string Path, bool Source, string Bucket, int Depth)>();
        stack.Push((root, true, "(racine)", 0));
        DateTimeOffset activity = default;
        int visited = 0;
        var deadline = settings.AnalysisBudgetSeconds <= 0 ? DateTimeOffset.MaxValue : DateTimeOffset.UtcNow.AddSeconds(settings.AnalysisBudgetSeconds);
        while (stack.Count > 0)
        {
            token.ThrowIfCancellationRequested();
            var item = stack.Pop();
            if (++visited > 1_000_000 || DateTimeOffset.UtcNow > deadline)
            {
                stats.Truncated = true;
                break;
            }
            try
            {
                foreach (var entry in new DirectoryInfo(item.Path).EnumerateFileSystemInfos())
                {
                    token.ThrowIfCancellationRequested();
                    if (DateTimeOffset.UtcNow > deadline)
                    {
                        stats.Truncated = true;
                        stack.Clear();
                        break;
                    }
                    if (entry.Attributes.HasFlag(FileAttributes.ReparsePoint))
                        continue;
                    if (entry is DirectoryInfo dir)
                    {
                        if (item.Depth >= 64)
                        {
                            stats.Truncated = true;
                            continue;
                        }
                        bool source = item.Source && !settings.Exclusions.Contains(dir.Name, StringComparer.OrdinalIgnoreCase) && independentRoots?.Contains(dir.FullName, StringComparer.OrdinalIgnoreCase) != true;
                        var bucket = item.Depth == 0 ? dir.Name : item.Bucket;
                        if (!buckets.ContainsKey(bucket))
                            buckets[bucket] = (dir.FullName, 0, Regenerable.Contains(bucket));
                        stack.Push((dir.FullName, source, bucket, item.Depth + 1));
                        if (source && (dir.Name.Contains("test", StringComparison.OrdinalIgnoreCase)))
                            stats.HasTests = true;
                    }
                    else if (entry is FileInfo file)
                    {
                        stats.TotalBytes += file.Length;
                        if (!buckets.TryGetValue(item.Bucket, out var b))
                            b = (root, 0, false);
                        buckets[item.Bucket] = (b.Path, b.Bytes + file.Length, b.Regenerable);
                        if (!item.Source)
                            continue;
                        stats.FileCount++;
                        stats.SourceBytes += file.Length;
                        if (file.LastWriteTimeUtc > activity)
                            activity = file.LastWriteTimeUtc;
                        if (file.Length > 50_000_000)
                            stats.LargeFiles++;
                        if (file.Name.StartsWith("LICENSE", StringComparison.OrdinalIgnoreCase) || file.Name.StartsWith("LICENCE", StringComparison.OrdinalIgnoreCase))
                            stats.HasLicense = true;
                        if (file.Name.Contains("test", StringComparison.OrdinalIgnoreCase))
                            stats.HasTests = true;
                        if (!SourceLanguages.Extensions.TryGetValue(file.Extension, out var language) || file.Length > 2_000_000 || file.Name.Contains(".min.") || file.Name.EndsWith(".g.cs") || file.Name.EndsWith(".generated.cs") || file.Name.EndsWith("lock.json") || file.Name == "package-lock.json")
                            continue;
                        if (!languages.TryGetValue(language, out var l))
                            languages[language] = l = new()
                            {
                                Language = language
                            };
                        var analyzed = await source.AnalyzeAsync(file, language, token);
                        l.Code += analyzed.Code;
                        l.Comments += analyzed.Comments;
                        l.Blank += analyzed.Blank;
                        stats.Todos.AddRange(analyzed.Todos.Take(Math.Max(0, 5000 - stats.Todos.Count)));
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { stats.Truncated = true; }
        }
        stats.Languages = languages.Values.OrderByDescending(x => x.Code).ToList();
        stats.Storage = buckets.Select(x => new StorageEntry(x.Key, x.Value.Path, x.Value.Bytes, x.Value.Regenerable)).OrderByDescending(x => x.Bytes).ToList();
        stats.AnalysisMilliseconds = clock.ElapsedMilliseconds;
        return (stats, activity);
    }
}
