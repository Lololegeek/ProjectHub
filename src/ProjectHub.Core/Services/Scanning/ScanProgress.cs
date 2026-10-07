namespace ProjectHub.Core.Services.Scanning;

public sealed record ScanProgress(int Directories, int Projects, long Files, TimeSpan Elapsed, string CurrentPath);
public sealed record ScanResult(List<ProjectHub.Core.Models.ProjectRecord> Projects, List<string> Errors, HashSet<string> CompletedRoots, bool Cancelled);
