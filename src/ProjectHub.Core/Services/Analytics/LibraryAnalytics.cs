using ProjectHub.Core.Models;
using ProjectHub.Core.Utilities;
namespace ProjectHub.Core.Services.Analytics;

public static class LibraryAnalytics
{
    public static List<ProjectRecord> StorageRoots(IEnumerable<ProjectRecord> projects)
    {
        var all = projects.ToArray();
        return all.Where(p => !all.Any(other => other.Id != p.Id && SafeFileSystem.Within(p.Path, other.Path))).ToList();
    }
    public static long TotalBytes(IEnumerable<ProjectRecord> projects) => StorageRoots(projects).Sum(p => p.Statistics.TotalBytes);
    public static string SizeLabel(IEnumerable<ProjectRecord> projects)
    {
        var roots = StorageRoots(projects);
        return (roots.Any(p => p.Statistics.MeasuredAt == default) ? "≥ " : roots.Any(p => p.Statistics.Truncated) ? "≈ " : "") + ProjectRecord.FormatSize(roots.Sum(p => p.Statistics.TotalBytes));
    }
}
