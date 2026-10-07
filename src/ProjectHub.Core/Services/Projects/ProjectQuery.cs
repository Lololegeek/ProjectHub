using System.Text.RegularExpressions;
using ProjectHub.Core.Models;
namespace ProjectHub.Core.Services.Projects;

public static partial class ProjectQuery
{
    [GeneratedRegex("(?:[^\\s\"]|\"[^\"]*\")+")] private static partial Regex Tokens();
    public static bool Matches(ProjectRecord p, string query)
    {
        foreach (Match m in Tokens().Matches(query))
        {
            string token = m.Value.Replace("\"", "");
            int colon = token.IndexOf(':');
            if (colon < 0)
            {
                if (!Contains(ProjectSearchDocument.Get(p), token))
                    return false;
                continue;
            }
            var key = token[..colon].ToLowerInvariant();
            var value = token[(colon + 1)..];
            bool match = key switch
            {
                "tag" => p.Tags.Concat(p.AutoTags).Any(x => Contains(x, value)),
                "language" or "lang" or "framework" or "stack" => p.Technologies.Concat(p.Statistics.Languages.Select(x => x.Language)).Any(x => Contains(Normalize(x), Normalize(value))),
                "git" => value == "dirty" ? p.Git.Dirty : value == "clean" ? p.Git.IsRepository && !p.Git.Dirty && p.Git.Error.Length == 0 : value == "none" ? !p.Git.IsRepository : Contains(p.Git.Branch, value),
                "favorite" => p.Favorite == (value is "true" or "1"),
                "pinned" => p.Pinned == (value is "true" or "1"),
                "archived" => p.Archived == (value is "true" or "1"),
                "missing" => p.Missing == (value is "true" or "1"),
                "readme" => (p.Readme.Length > 0) == (value is "true" or "1"),
                "collection" => p.Collections.Any(x => Contains(x, value)),
                "modified" => CompareAge(p.LastActivity, value),
                "size" => CompareSize(p.Statistics.TotalBytes, value),
                "path" => Contains(p.Path, value),
                _ => Contains(ProjectSearchDocument.Get(p), token)
            };
            if (!match)
                return false;
        }
        return true;
    }
    private static bool Contains(string source, string value) => source.Contains(value, StringComparison.OrdinalIgnoreCase);
    private static string Normalize(string value) => value.ToLowerInvariant().Replace("c#", "csharp").Replace("f#", "fsharp").Replace(".js", "");
    private static bool CompareAge(DateTimeOffset date, string value)
    {
        if (date == default || value.Length < 3 || value[0] is not ('<' or '>'))
            return false;
        if (!double.TryParse(value[1..^1], System.Globalization.CultureInfo.InvariantCulture, out var amount))
            return false;
        var days = amount * (value[^1] switch
        {
            'd' => 1,
            'w' => 7,
            'm' => 30.44,
            'y' => 365.25,
            _ => double.NaN
        });
        return value[0] == '<' ? (DateTimeOffset.UtcNow - date).TotalDays < days : (DateTimeOffset.UtcNow - date).TotalDays > days;
    }
    private static bool CompareSize(long size, string value)
    {
        var m = Regex.Match(value, @"^([<>])([\d.]+)(kb|mb|gb|tb|b)?$", RegexOptions.IgnoreCase);
        if (!m.Success || !double.TryParse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture, out var number))
            return false;
        number *= m.Groups[3].Value.ToLowerInvariant() switch
        {
            "kb" => 1024,
            "mb" => 1024 * 1024,
            "gb" => 1024L * 1024 * 1024,
            "tb" => 1024L * 1024 * 1024 * 1024,
            _ => 1
        };
        return m.Groups[1].Value == ">" ? size > number : size < number;
    }
    public static int FuzzyScore(string source, string query)
    {
        int index = 0, score = 0;
        source = source.ToLowerInvariant();
        foreach (char c in query.ToLowerInvariant())
        {
            int found = source.IndexOf(c, index);
            if (found < 0)
                return -1;
            score += found == index ? 4 : 1;
            index = found + 1;
        }
        return score;
    }
}
