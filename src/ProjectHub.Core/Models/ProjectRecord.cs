namespace ProjectHub.Core.Models;

public sealed class ProjectRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTimeOffset FirstDetected { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastActivity
    {
        get; set;
    }
    public bool Favorite
    {
        get; set;
    }
    public bool Pinned
    {
        get; set;
    }
    public int PinOrder
    {
        get; set;
    }
    public bool Archived
    {
        get; set;
    }
    public bool Missing
    {
        get; set;
    }
    public string Notes { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public List<string> Screenshots { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public List<string> AutoTags { get; set; } = [];
    public List<string> Collections { get; set; } = [];
    public List<string> Technologies { get; set; } = [];
    public List<ProjectComponent> Components { get; set; } = [];
    public List<string> IndependentSubprojects { get; set; } = [];
    public List<LaunchProfile> Commands { get; set; } = [];
    public GitSnapshot Git { get; set; } = new();
    public ProjectStatistics Statistics { get; set; } = new();
    public string Readme { get; set; } = "";
    public string Fingerprint { get; set; } = "";
    public int SignatureBytes
    {
        get; set;
    }
    public List<ActivityEvent> Activity { get; set; } = [];
    public string Stack => string.Join(" · ", Technologies.Take(4));
    public string FullStack => string.Join(" · ", Technologies);
    public string Initials => new string(Name.Split([' ', '-', '_'], StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x => char.ToUpperInvariant(x[0])).ToArray());
    public string SizeLabel => Statistics.MeasuredAt == default ? "À analyser" : (Statistics.Truncated ? "≈ " : "") + FormatSize(Statistics.TotalBytes);
    public override string ToString() => Name;
    public string ActivityLabel => Missing ? "Dossier indisponible" : LastActivity == default ? "Activité inconnue" : LastActivity.ToLocalTime().ToString("dd MMM yyyy");
    public string GitLabel => !Git.IsRepository ? "Local" : Git.Error.Length > 0 ? "Git indisponible" : Git.Dirty ? $"{Git.ChangedFiles.Count} modifications" : "Git propre";
    public string BranchLabel => Git.Branch.Length == 0 ? "Sans dépôt" : Git.Branch;
    public string FavoriteLabel => Favorite ? "★" : "☆";
    public bool Forgotten => LastActivity != default && LastActivity < DateTimeOffset.UtcNow.AddMonths(-6);
    public static string FormatSize(long bytes) => bytes >= 1L << 30 ? $"{bytes / (double)(1L << 30):0.0} Go" : bytes >= 1 << 20 ? $"{bytes / (double)(1 << 20):0.0} Mo" : $"{bytes / 1024.0:0} Ko";
}
