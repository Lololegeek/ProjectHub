using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ProjectHub.Core.Models;
using ProjectHub.Core.Providers;
using ProjectHub.Core.Services.Analytics;
using ProjectHub.Core.Services.Projects;
using Windows.Storage.Pickers;
namespace ProjectHub.App.Views.Details;

public sealed partial class ProjectDetailsView : UserControl
{
    private readonly ProjectRecord project;
    private readonly ViewModels.ProjectDetailsViewModel viewModel;
    public void ShowSection(int index) => DetailsTabs.SelectedIndex = index;
    public ProjectDetailsView(ProjectRecord project)
    {
        this.project = project;
        viewModel = new(project);
        InitializeComponent();
        DataContext = project;
        Editors.ItemsSource = App.Tools;
        Favorite.IsChecked = project.Favorite;
        Pinned.IsChecked = project.Pinned;
        Archived.IsChecked = project.Archived;
        PinOrder.Value = project.PinOrder;
        Notes.Text = project.Notes;
        Tags.Text = string.Join(", ", project.Tags);
        Collections.Text = string.Join(", ", project.Collections);
        Populate();
    }
    private void Populate()
    {
        var s = project.Statistics;
        var g = project.Git;
        GeneralStats.Text = viewModel.GeneralSummary;
        HealthText.Text = viewModel.HealthSummary;
        AutoTags.Text = "Tags automatiques : " + string.Join(", ", project.AutoTags);
        Components.ItemsSource = project.Components;
        GitSummary.Text = viewModel.GitSummary;
        GitChanges.ItemsSource = g.ChangedFiles;
        Commits.ItemsSource = g.Commits;
        LanguageStats.ItemsSource = s.Languages;
        Todos.ItemsSource = s.Todos;
        TodoSummary.Text = string.Join(" · ", s.Todos.GroupBy(x => x.Kind).Select(x => $"{x.Key} {x.Count()}"));
        Activity.ItemsSource = project.Activity.ToArray();
        Storage.ItemsSource = s.Storage.Select(x => new { x.Name, Bytes = ProjectRecord.FormatSize(x.Bytes), Regenerable = x.Regenerable ? "Régénérable" : "" }).ToList();
        StorageSummary.Text = $"Total : {project.SizeLabel} · Sources : {ProjectRecord.FormatSize(s.SourceBytes)}\nPotentiellement régénérable : {ProjectRecord.FormatSize(s.RecoverableBytes)}";
        Commands.ItemsSource = project.Commands;
        Screenshots.ItemsSource = project.Screenshots;
        GitHubButton.IsEnabled = g.WebUrl.Length > 0;
        Controls.MarkdownRenderer.Render(ReadmeHost, project.Readme, project.Path);
    }
    public async Task SaveAsync()
    {
        await viewModel.SaveAsync(Favorite.IsChecked == true, Pinned.IsChecked == true, Archived.IsChecked == true, double.IsNaN(PinOrder.Value) ? 0 : (int)PinOrder.Value, Notes.Text, Tags.Text, Collections.Text);
    }
    private void Open_Click(object s, RoutedEventArgs e) => App.Main.OpenEditor(project);
    private void Terminal_Click(object s, RoutedEventArgs e) => App.Main.Launch(() => App.Launcher.OpenTerminal(project), project, "Terminal ouvert");
    private void Folder_Click(object s, RoutedEventArgs e) => App.Main.Launch(() => App.Launcher.OpenFolder(project), project, "Dossier ouvert");
    private void GitHub_Click(object s, RoutedEventArgs e) => App.Main.Launch(() => App.Launcher.OpenGitHub(project), project, "GitHub ouvert");
    private void Editor_Changed(object s, SelectionChangedEventArgs e)
    {
        if (Editors.SelectedItem is InstalledTool tool)
            App.Main.Launch(() => App.Launcher.OpenTool(project, tool), project, "Ouvert dans " + tool.Name);
    }
    private async void Refresh_Click(object s, RoutedEventArgs e)
    {
        try
        {
            await App.Engine.RefreshProjectAsync(project);
            Populate();
        }
        catch (Exception ex) { GeneralStats.Text = ex.Message; }
    }
    private void Todo_Click(object s, ItemClickEventArgs e)
    {
        var item = (TodoItem)e.ClickedItem;
        App.Main.Launch(() => App.Launcher.OpenFile(item.Path, item.Line, App.Tools.FirstOrDefault(x => x.Name == "VS Code") ?? App.Tools.FirstOrDefault(x => x.Kind == "Editor")), project, "Fichier ouvert : " + item.Path);
    }
    private async void Command_Click(object s, ItemClickEventArgs e)
    {
        var profile = (LaunchProfile)e.ClickedItem;
        try
        {
            await App.Runner.StartAsync(project, profile);
            project.LastActivity = DateTimeOffset.UtcNow;
            ProjectEnricher.AddActivity(project, "Commande", profile.Name);
            await App.Engine.SaveProjectAsync(project);
            CommandStatus.Text = "Tâche lancée. Consultez la page Tâches pour sa sortie ou pour l’arrêter.";
        }
        catch (Exception ex) { CommandStatus.Text = ex.Message; }
    }
    private async Task<string?> PickImage()
    {
        var picker = new FileOpenPicker();
        foreach (var ext in new[] { ".png", ".jpg", ".jpeg", ".webp" })
            picker.FileTypeFilter.Add(ext);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.Main));
        return (await picker.PickSingleFileAsync())?.Path;
    }
    private async void Image_Click(object s, RoutedEventArgs e)
    {
        try
        {
            if (await PickImage() is string path)
            {
                project.ImagePath = path;
                await App.Engine.SaveProjectAsync(project);
            }
        }
        catch (Exception ex) { GeneralStats.Text = ex.Message; }
    }
    private async void Screenshot_Click(object s, RoutedEventArgs e)
    {
        try
        {
            if (await PickImage() is string path)
            {
                project.Screenshots.Add(path);
                Screenshots.ItemsSource = project.Screenshots.ToArray();
                await App.Engine.SaveProjectAsync(project);
            }
        }
        catch (Exception ex) { GeneralStats.Text = ex.Message; }
    }
    private void Cleanup_Click(object s, RoutedEventArgs e)
    {
        var list = project.Statistics.Storage.Where(x => x.Regenerable).ToList();
        StorageSummary.Text = "Dossiers régénérables à examiner dans l’Explorateur :\n" + string.Join('\n', list.Select(x => $"{x.Path} · {ProjectRecord.FormatSize(x.Bytes)}"));
        CleanupReview.Children.Clear();
        foreach (var entry in list)
        {
            var confirm = new CheckBox { Content = "Confirmer : " + entry.Path + " (" + ProjectRecord.FormatSize(entry.Bytes) + ")" };
            var button = new Button { Content = "Envoyer ce dossier à la corbeille", IsEnabled = false };
            confirm.Checked += (_, _) => button.IsEnabled = true;
            confirm.Unchecked += (_, _) => button.IsEnabled = false;
            button.Click += async (_, _) =>
            {
                try
                {
                    button.IsEnabled = false;
                    Services.CleanupService.Recycle(project, entry);
                    await App.Engine.RefreshProjectAsync(project);
                    Populate();
                    CleanupReview.Children.Clear();
                }
                catch (Exception ex) { StorageSummary.Text = ex.Message; button.IsEnabled = true; }
            };
            CleanupReview.Children.Add(confirm);
            CleanupReview.Children.Add(button);
        }
    }
}
