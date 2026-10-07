using System.Text.RegularExpressions;
using ProjectHub.Core.Models;
namespace ProjectHub.Core.Services.Analytics;

public sealed record DuplicateSuggestion(ProjectRecord First, ProjectRecord Second, string Reason);
public static partial class DuplicateFinder
{
    [GeneratedRegex(@"[\s_-]*(old|backup|copy|copie|bak|v\d+|\(\d+\))$", RegexOptions.IgnoreCase)] private static partial Regex Suffix();
    public static List<DuplicateSuggestion> Find(IEnumerable<ProjectRecord> projects)
    {
        var all = projects.ToList();
        var result = new List<DuplicateSuggestion>();
        for (int i = 0; i < all.Count; i++)
            for (int j = i + 1; j < all.Count; j++)
            {
                var a = all[i];
                var b = all[j];
                string reason = "";
                if (a.Git.Remote.Length > 0 && a.Git.Remote.Equals(b.Git.Remote, StringComparison.OrdinalIgnoreCase))
                    reason = "Même remote Git";
                else if (a.Fingerprint.Length > 0 && a.Fingerprint == b.Fingerprint)
                    reason = "Manifestes identiques";
                else if (Suffix().Replace(a.Name, "").Equals(Suffix().Replace(b.Name, ""), StringComparison.OrdinalIgnoreCase) && a.Technologies.Intersect(b.Technologies).Any())
                    reason = "Nom et stack similaires";
                if (reason.Length > 0)
                    result.Add(new(a, b, reason));
            }
        return result;
    }
}
