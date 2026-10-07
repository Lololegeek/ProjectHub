using System.Text.Json;
using ProjectHub.Core.Models;
namespace ProjectHub.Core.Services.Persistence;

public sealed record HubExport(int Version, HubSettings Settings, List<ProjectCustomization> Projects);
public static class ConfigurationTransfer
{
    public static async Task ExportAsync(string path, HubSettings settings, IEnumerable<ProjectRecord> projects)
    {
        var export = new HubExport(1, settings, projects.Select(p => new ProjectCustomization(p.Path, p.Favorite, p.Pinned, p.PinOrder, p.Archived, p.Notes, p.Tags, p.Collections, p.ImagePath, p.Screenshots)).ToList());
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true }));
    }
    public static async Task<HubExport> ReadAsync(string path)
    {
        if (new FileInfo(path).Length > 10_000_000)
            throw new IOException("Configuration trop volumineuse");
        var export = JsonSerializer.Deserialize<HubExport>(await File.ReadAllTextAsync(path)) ?? throw new IOException("Configuration invalide");
        if (export.Version != 1)
            throw new IOException("Version de configuration non prise en charge");
        return export;
    }
    public static void Apply(HubExport import, HubSettings target, List<ProjectRecord> projects)
    {
        target.Roots = import.Settings.Roots.Where(Path.IsPathFullyQualified).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        target.Exclusions = import.Settings.Exclusions;
        target.Collections = import.Settings.Collections;
        target.Theme = import.Settings.Theme;
        target.MaxDepth = Math.Clamp(import.Settings.MaxDepth, 1, 64);
        target.Concurrency = Math.Clamp(import.Settings.Concurrency, 1, 8);
        target.ToolOverrides = import.Settings.ToolOverrides;
        target.PreferredEditor = import.Settings.PreferredEditor;
        target.VisibleColumns = import.Settings.VisibleColumns;
        target.ViewMode = import.Settings.ViewMode;
        target.IncludeHidden = import.Settings.IncludeHidden;
        target.FollowSymlinks = import.Settings.FollowSymlinks;
        target.WatchEnabled = import.Settings.WatchEnabled;
        target.BackgroundScan = import.Settings.BackgroundScan;
        target.TrayEnabled = import.Settings.TrayEnabled;
        target.HotkeyEnabled = import.Settings.HotkeyEnabled;
        target.HotkeyModifiers = import.Settings.HotkeyModifiers;
        target.HotkeyKey = import.Settings.HotkeyKey;
        target.GitExecutable = import.Settings.GitExecutable;
        target.GitRefreshSeconds = Math.Clamp(import.Settings.GitRefreshSeconds, 30, 3600);
        target.AnalysisBudgetSeconds = Math.Clamp(import.Settings.AnalysisBudgetSeconds, 0, 3600);
        foreach (var data in import.Projects)
        {
            var p = projects.FirstOrDefault(x => x.Path.Equals(data.Path, StringComparison.OrdinalIgnoreCase));
            if (p == null)
                continue;
            data.ApplyTo(p);
        }
        target.PendingCustomizations = import.Projects.Concat(import.Settings.PendingCustomizations).Where(data => !projects.Any(p => p.Path.Equals(data.Path, StringComparison.OrdinalIgnoreCase))).DistinctBy(x => x.Path, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
