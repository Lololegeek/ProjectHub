using ProjectHub.Core.Models;
using ProjectHub.Core.Services.Analytics;
namespace ProjectHub.App.ViewModels;

public sealed class ProjectDetailsViewModel(ProjectRecord project)
{
    public ProjectRecord Project { get; } = project;
    public string GeneralSummary
    {
        get
        {
            var s = Project.Statistics;
            return $"{Project.SizeLabel} sur disque · {s.FileCount:N0} fichiers source · {s.Lines:N0} lignes\nDétecté le {Project.FirstDetected.LocalDateTime:d} · Activité : {Project.ActivityLabel}" + (s.Truncated ? "\nAnalyse partielle : fichiers inaccessibles ou limite atteinte." : "");
        }
    }
    public string HealthSummary
    {
        get
        {
            var health = ProjectHealth.Evaluate(Project);
            return $"Santé du projet : {health.Score}/100 · indicateur documentaire\n" + string.Join('\n', health.Reasons);
        }
    }
    public string GitSummary
    {
        get
        {
            var g = Project.Git;
            return !g.IsRepository ? "Projet local sans dépôt Git." : g.Error.Length > 0 ? g.Error : $"Branche : {g.Branch} · +{g.Ahead} / −{g.Behind} · {g.BranchCount} branches\n{g.Remote}\n{g.ChangedFiles.Count(x => x.Status == "??")} non suivis · {g.ChangedFiles.Count(x => x.Status.Length > 0 && x.Status[0] is not ('.' or '?'))} staged";
        }
    }
    public async Task SaveAsync(bool favorite, bool pinned, bool archived, int order, string notes, string tags, string collections)
    {
        Project.Favorite = favorite;
        Project.Pinned = pinned;
        Project.Archived = archived;
        Project.PinOrder = order;
        Project.Notes = notes;
        Project.Tags = Split(tags);
        Project.Collections = Split(collections);
        foreach (var name in Project.Collections)
            if (!App.Engine.Settings.Collections.Any(x => x.Name == name))
                App.Engine.Settings.Collections.Add(new(name, ""));
        await App.Engine.Repository.SaveSettingsAsync(App.Engine.Settings);
        await App.Engine.SaveProjectAsync(Project);
    }
    private static List<string> Split(string text) => text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
}
