using System.Runtime.CompilerServices;
using ProjectHub.Core.Models;
namespace ProjectHub.Core.Services.Projects;

public static class ProjectSearchDocument
{
    private sealed record Entry(string Name, string Path, string Description, string Readme, string Notes, string Remote, List<string> Technologies, List<string> Tags, List<string> AutoTags, ProjectStatistics Stats, string Document)
    {
        public bool Matches(ProjectRecord p) => Name == p.Name && Path == p.Path && Description == p.Description && Readme == p.Readme && Notes == p.Notes && Remote == p.Git.Remote && ReferenceEquals(Technologies, p.Technologies) && ReferenceEquals(Tags, p.Tags) && ReferenceEquals(AutoTags, p.AutoTags) && ReferenceEquals(Stats, p.Statistics);
    }
    private static readonly ConditionalWeakTable<ProjectRecord, Entry> entries = new();
    private static readonly object gate = new();
    public static string Get(ProjectRecord p)
    {
        lock (gate)
        {
            if (entries.TryGetValue(p, out var cached) && cached.Matches(p))
                return cached.Document;
            var document = string.Join(' ', p.Name, p.Path, p.Description, p.FullStack, string.Join(' ', p.Tags.Concat(p.AutoTags)), p.Git.Remote, p.Notes, p.Readme, string.Join(' ', p.Statistics.Languages.Select(x => x.Language)));
            entries.Remove(p);
            entries.Add(p, new(p.Name, p.Path, p.Description, p.Readme, p.Notes, p.Git.Remote, p.Technologies, p.Tags, p.AutoTags, p.Statistics, document));
            return document;
        }
    }
}
