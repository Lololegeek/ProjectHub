using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using ProjectHub.Core.Models;
namespace ProjectHub.Core.Services.Analytics;

public sealed partial class SourceFileAnalyzer
{
    private sealed record CachedFile(long Bytes, long Ticks, long Code, long Comments, long Blank, List<TodoItem> Todos);
    private readonly ConcurrentDictionary<string, CachedFile> cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object cacheGate = new();
    private long cacheBytes;
    [GeneratedRegex(@"\b(TODO|FIXME|HACK|XXX)\b", RegexOptions.CultureInvariant)] private static partial Regex Marker();
    public async Task<(long Code, long Comments, long Blank, List<TodoItem> Todos)> AnalyzeAsync(FileInfo file, string language, CancellationToken token)
    {
        if (cache.TryGetValue(file.FullName, out var cached) && cached.Bytes == file.Length && cached.Ticks == file.LastWriteTimeUtc.Ticks)
            return (cached.Code, cached.Comments, cached.Blank, cached.Todos);
        long code = 0, comments = 0, blank = 0;
        var todos = new List<TodoItem>();
        bool block = false;
        int number = 0;
        await using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = await reader.ReadLineAsync(token)) != null)
        {
            number++;
            if (line.Contains('\0'))
                break;
            var text = line.Trim();
            bool hashComment = language is "Python" or "GDScript" or "PowerShell" or "Shell" or "YAML" or "TOML" or "Ruby";
            bool xml = language is "HTML" or "XML" or "XAML";
            if (text.Length == 0)
                blank++;
            else if (block || text.StartsWith("//") || hashComment && text.StartsWith('#') || language == "Luau" && text.StartsWith("--") || text.StartsWith("/*") || xml && text.StartsWith("<!--"))
                comments++;
            else
                code++;
            if (text.StartsWith("/*") && !text.Contains("*/"))
                block = true;
            if (text.Contains("*/"))
                block = false;
            if (text.Length > 8192)
                continue;
            if (todos.Count < 100)
                foreach (Match match in Marker().Matches(line))
                    todos.Add(new(match.Value, file.FullName, number, text.Length > 240 ? text[..240] : text));
        }
        lock (cacheGate)
        {
            long Estimate(CachedFile item) => 512 + file.FullName.Length * 2 + item.Todos.Sum(t => 128 + t.Text.Length * 2);
            var entry = new CachedFile(file.Length, file.LastWriteTimeUtc.Ticks, code, comments, blank, todos);
            if (cache.TryGetValue(file.FullName, out var previous))
                cacheBytes -= Estimate(previous);
            cacheBytes += Estimate(entry);
            if (cacheBytes > 32_000_000 || cache.Count > 150000)
            {
                cache.Clear();
                cacheBytes = Estimate(entry);
            }
            cache[file.FullName] = entry;
        }
        return (code, comments, blank, todos);
    }
}
