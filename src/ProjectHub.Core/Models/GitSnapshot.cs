namespace ProjectHub.Core.Models;

public sealed class GitSnapshot
{
    public bool IsRepository
    {
        get; set;
    }
    public string Branch { get; set; } = "";
    public string Remote { get; set; } = "";
    public string Error { get; set; } = "";
    public int Ahead
    {
        get; set;
    }
    public int Behind
    {
        get; set;
    }
    public int BranchCount
    {
        get; set;
    }
    public List<GitChange> ChangedFiles { get; set; } = [];
    public List<GitCommit> Commits { get; set; } = [];
    public bool Dirty => ChangedFiles.Count > 0;
    public string WebUrl
    {
        get
        {
            var remote = Remote.Trim();
            if (remote.StartsWith("git@github.com:"))
                remote = "https://github.com/" + remote[15..];
            if (remote.StartsWith("ssh://git@github.com/"))
                remote = "https://github.com/" + remote[21..];
            return Uri.TryCreate(remote, UriKind.Absolute, out var uri) && uri.Scheme == "https" && uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ? remote.EndsWith(".git") ? remote[..^4] : remote : "";
        }
    }
}
public sealed record GitChange(string Status, string Path);
public sealed record GitCommit(string Hash, string Author, DateTimeOffset Date, string Subject);
