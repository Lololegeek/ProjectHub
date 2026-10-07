namespace ProjectHub.Core.Models;

public sealed class HubSettings
{
    public List<string> Roots { get; set; } = [];
    public List<string> Exclusions { get; set; } = ["node_modules", ".git", ".vscode", ".idea", ".vs", "bin", "obj", "target", "dist", "build", ".next", ".nuxt", "coverage", ".cache", ".gradle", "venv", ".venv", "__pycache__", "Library", "Temp", "Logs", ".godot", ".unity", "vendor", "Packages"];
    public int MaxDepth { get; set; } = 12;
    public int Concurrency { get; set; } = 3;
    public int AnalysisBudgetSeconds { get; set; } = 120;
    public List<string> VisibleColumns { get; set; } = ["Stack", "Git", "Size", "Activity", "Path"];
    public bool FollowSymlinks
    {
        get; set;
    }
    public bool IncludeHidden { get; set; } = true;
    public bool WatchEnabled { get; set; } = true;
    public bool BackgroundScan { get; set; } = true;
    public bool TrayEnabled
    {
        get; set;
    }
    public bool HotkeyEnabled { get; set; } = true;
    public uint HotkeyModifiers { get; set; } = 3;
    public uint HotkeyKey { get; set; } = 0x50;
    public int GitRefreshSeconds { get; set; } = 120;
    public string GitExecutable { get; set; } = "git";
    public string Theme { get; set; } = "Dark";
    public string Language { get; set; } = "fr";
    public string ViewMode { get; set; } = "Cards";
    public string PreferredEditor { get; set; } = "";
    public Dictionary<string, string> ToolOverrides { get; set; } = [];
    public List<CollectionDefinition> Collections { get; set; } = [];
    public List<ProjectCustomization> PendingCustomizations { get; set; } = [];
}
public sealed record CollectionDefinition(string Name, string Query);
