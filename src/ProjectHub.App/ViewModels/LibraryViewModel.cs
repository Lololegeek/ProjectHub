using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using ProjectHub.Core.Models;
using ProjectHub.Core.Services.Projects;
using ProjectHub.Core.Services.Scanning;
namespace ProjectHub.App.ViewModels;

public partial class LibraryViewModel : ObservableObject
{
    private readonly DispatcherQueue dispatcher;
    public ObservableCollection<ProjectRecord> VisibleProjects { get; } = [];
    public ObservableCollection<string> Stacks { get; } = ["Toutes les stacks"];
    [ObservableProperty] public partial string Query { get; set; } = "";
    [ObservableProperty] public partial string Scope { get; set; } = "all";
    [ObservableProperty] public partial string SelectedStack { get; set; } = "Toutes les stacks";
    [ObservableProperty] public partial string Sort { get; set; } = "Activité récente";
    [ObservableProperty] public partial string Status { get; set; } = "Chargement de l’index…";
    [ObservableProperty] public partial string Summary { get; set; } = "";
    [ObservableProperty]
    public partial bool IsScanning
    {
        get; set;
    }
    [ObservableProperty] public partial string PageTitle { get; set; } = "Bibliothèque";
    [ObservableProperty] public partial string PageSubtitle { get; set; } = "Tous vos projets, au même endroit.";
    [ObservableProperty]
    public partial int ProjectCount
    {
        get; set;
    }
    [ObservableProperty]
    public partial int DirtyCount
    {
        get; set;
    }
    [ObservableProperty]
    public partial int ForgottenCount
    {
        get; set;
    }
    [ObservableProperty] public partial string TotalSize { get; set; } = "0 Ko";
    private CancellationTokenSource? scan;
    public LibraryViewModel(DispatcherQueue dispatcher)
    {
        this.dispatcher = dispatcher;
        App.Engine.Updated += () => dispatcher.TryEnqueue(Refresh);
    }
    partial void OnQueryChanged(string value) => Refresh();
    partial void OnScopeChanged(string value) => Refresh();
    partial void OnSelectedStackChanged(string value) => Refresh();
    partial void OnSortChanged(string value) => Refresh();
    public async Task InitializeAsync()
    {
        await Task.Run(() => App.Engine.InitializeAsync());
        App.Tools = await Task.Run(() => new ProjectHub.Core.Providers.ToolProvider().Detect(App.Engine.Settings));
        foreach (var stack in App.Engine.Projects.SelectMany(p => p.Technologies).Distinct().Order())
            Stacks.Add(stack);
        Refresh();
        Status = App.Engine.Settings.Roots.Count == 0 ? "Ajoutez un dossier pour découvrir vos projets." : "Index prêt · " + App.Engine.Projects.Count + " projets";
    }
    public void Refresh()
    {
        var all = App.Engine.Projects.ToArray();
        ProjectCount = all.Length;
        DirtyCount = all.Count(p => p.Git.Dirty);
        ForgottenCount = all.Count(p => p.Forgotten);
        TotalSize = ProjectHub.Core.Services.Analytics.LibraryAnalytics.SizeLabel(all);
        IEnumerable<ProjectRecord> items = all.Where(p => Scope == "archived" ? p.Archived : !p.Archived);
        items = Scope switch
        {
            "favorites" => items.Where(p => p.Favorite),
            "pinned" => items.Where(p => p.Pinned),
            "recent" => items.Where(p => p.LastActivity > DateTimeOffset.UtcNow.AddDays(-30)),
            "dirty" => items.Where(p => p.Git.Dirty),
            "forgotten" => items.Where(p => p.Forgotten),
            "dashboard" => items.OrderByDescending(p => p.LastActivity).Take(12),
            _ => items
        };
        if (Scope.StartsWith("collection:"))
        {
            var name = Scope[11..];
            var def = App.Engine.Settings.Collections.FirstOrDefault(x => x.Name == name);
            items = items.Where(p => def?.Query.Length > 0 ? ProjectQuery.Matches(p, def.Query) : p.Collections.Contains(name));
        }
        items = items.Where(p => ProjectQuery.Matches(p, Query) && (SelectedStack == "Toutes les stacks" || p.Technologies.Contains(SelectedStack)));
        items = Sort switch
        {
            "Nom" => items.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase),
            "Taille" => items.OrderByDescending(p => p.Statistics.TotalBytes),
            "Lignes de code" => items.OrderByDescending(p => p.Statistics.Lines),
            _ => items.OrderByDescending(p => p.Pinned).ThenBy(p => p.PinOrder).ThenByDescending(p => p.LastActivity)
        };
        var visible = items.ToArray();
        if (visible.Length == VisibleProjects.Count && visible.Select(x => x.Id).SequenceEqual(VisibleProjects.Select(x => x.Id)))
        {
            for (int i = 0; i < visible.Length; i++)
                VisibleProjects[i] = visible[i];
        }
        else
        {
            VisibleProjects.Clear();
            foreach (var p in visible)
                VisibleProjects.Add(p);
        }
        foreach (var stack in all.SelectMany(p => p.Technologies).Distinct().Order())
            if (!Stacks.Contains(stack))
                Stacks.Add(stack);
        Summary = $"{VisibleProjects.Count} projets affichés · {all.Count(p => p.Missing)} indisponibles";
    }
    public async Task ScanAsync()
    {
        if (IsScanning)
            return;
        scan = new();
        IsScanning = true;
        try
        {
            var progress = new Progress<ScanProgress>(p => { Status = p.CurrentPath.StartsWith("Analyse") ? p.CurrentPath + $" · {p.Elapsed:mm\\:ss}" : $"{p.Directories:N0} dossiers · {p.Projects} projets · {p.Files:N0} fichiers · {p.Elapsed:mm\\:ss}"; });
            await App.Engine.ScanAsync(progress, scan.Token);
            Status = $"Index actualisé · {App.Engine.Projects.Count} projets · {App.Engine.ScanErrors.Count} erreurs · {App.Engine.LastScanDuration.TotalSeconds:0.0}s";
        }
        catch (Exception e) { Status = e.Message; App.Engine.Log.Write("Scanning", e.ToString()); }
        finally { IsScanning = false; scan.Dispose(); scan = null; Refresh(); }
    }
    public void CancelScan() => scan?.Cancel();
}
