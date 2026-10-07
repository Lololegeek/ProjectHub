using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using ProjectHub.App.ViewModels;
using ProjectHub.App.Views.Analytics;
using ProjectHub.App.Views.Settings;
using ProjectHub.App.Views.Details;
using ProjectHub.App.Views.Tasks;
using ProjectHub.App.Views.Collections;
using ProjectHub.Core.Models;
using ProjectHub.Core.Services.Projects;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.System;
namespace ProjectHub.App;

public sealed partial class MainWindow : Window
{
    public LibraryViewModel ViewModel
    {
        get;
    }
    private readonly Infrastructure.DesktopIntegration desktop;
    private bool ready;
    public MainWindow()
    {
        ViewModel = new(DispatcherQueue);
        InitializeComponent();
        Root.DataContext = ViewModel;
        Title = "ProjectHub";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBar);
        SystemBackdrop = new MicaBackdrop();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1380, 900));
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/ProjectHub.ico"));
        desktop = new(this);
        AppWindow.Closing += (_, e) => { if (App.Engine.Settings.TrayEnabled) { e.Cancel = true; AppWindow.Hide(); } else CloseApp(); };
        App.Runner.Completed += task => DispatcherQueue.TryEnqueue(async () =>
        {
            var p = App.Engine.Projects.FirstOrDefault(x => x.Id == task.ProjectId);
            if (p == null)
                return;
            p.LastActivity = DateTimeOffset.UtcNow;
            ProjectEnricher.AddActivity(p, "Commande", $"{task.Name} · code {task.ExitCode} · {task.Duration.TotalSeconds:0.0}s");
            await App.Engine.SaveProjectAsync(p);
        });
        Closed += (_, _) => CloseApp();
        var palette = new KeyboardAccelerator { Key = VirtualKey.K, Modifiers = VirtualKeyModifiers.Control };
        palette.Invoked += async (_, e) => { e.Handled = true; await ShowPaletteAsync(); };
        Root.KeyboardAccelerators.Add(palette);
        var search = new KeyboardAccelerator { Key = VirtualKey.F, Modifiers = VirtualKeyModifiers.Control };
        search.Invoked += (_, e) => { e.Handled = true; ShowLibrary("all"); Library.FocusSearch(); };
        Root.KeyboardAccelerators.Add(search);
        Root.Loaded += Initialize;
    }
    private async void Initialize(object sender, RoutedEventArgs e)
    {
        if (ready)
            return;
        ready = true;
        try
        {
            await ViewModel.InitializeAsync();
            ApplySettings();
            Library.ApplyPreferences();
            Navigation.SelectedItem = Navigation.MenuItems.OfType<NavigationViewItem>().First(x => x.Tag?.ToString() == "all");
            if (App.Engine.Settings.Roots.Count == 0)
            {
                var welcome = new ContentDialog { XamlRoot = Root.XamlRoot, Title = "Bienvenue dans ProjectHub", Content = new TextBlock { Text = "Choisissez les dossiers qui contiennent vos projets. ProjectHub détectera leurs stacks et les regroupera automatiquement.", TextWrapping = TextWrapping.Wrap, MaxWidth = 440 }, PrimaryButtonText = "Choisir un dossier", CloseButtonText = "Plus tard", DefaultButton = ContentDialogButton.Primary };
                if (await welcome.ShowAsync() == ContentDialogResult.Primary)
                {
                    await AddRootAsync();
                    await ViewModel.ScanAsync();
                }
            }
        }
        catch (Exception ex) { ViewModel.Status = "Initialisation : " + ex.Message; App.Engine.Log.Write("Errors", ex.ToString()); }
    }
    public void ApplySettings()
    {
        App.Language.SetLanguage(App.Engine.Settings.Language);
        Root.RequestedTheme = App.Engine.Settings.Theme switch
        {
            "Light" => ElementTheme.Light,
            "System" => ElementTheme.Default,
            _ => ElementTheme.Dark
        };
        App.Language.Apply(Root);
        desktop.Configure(App.Engine.Settings);
    }
    public void ApplyLocalization() => App.Language.Apply(Root);
    private void CloseApp()
    {
        desktop.Dispose();
        App.Runner.Dispose();
        App.Engine.Dispose();
    }
    public void ExitApp()
    {
        App.Engine.Settings.TrayEnabled = false;
        Close();
    }
    public void ShowSearch()
    {
        AppWindow.Show();
        Activate();
        ShowLibrary("all");
        Library.FocusSearch();
    }
    private void Navigation_Changed(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (Library == null)
            return;
        if (args.IsSettingsSelected)
        {
            ShowPage(new SettingsPage());
            return;
        }
        var tag = (args.SelectedItem as NavigationViewItem)?.Tag?.ToString() ?? "all";
        switch (tag)
        {
            case "analytics":
                ShowPage(new AnalyticsPage());
                break;
            case "settings":
                ShowPage(new SettingsPage());
                break;
            case "tasks":
                ShowPage(new TasksPage());
                break;
            case "diagnostics":
                ShowPage(new DiagnosticsPage());
                break;
            case "collections":
                ShowPage(new CollectionsPage());
                break;
            default:
                ShowLibrary(tag);
                break;
        }
    }
    public void ShowPage(UserControl page)
    {
        Library.Visibility = Visibility.Collapsed;
        DashboardStrip.Visibility = Visibility.Collapsed;
        PageHost.Visibility = Visibility.Visible;
        PageHost.Content = page;
        App.Language.Apply(Root);
    }
    public void ShowLibrary(string scope)
    {
        PageHost.Visibility = Visibility.Collapsed;
        PageHost.Content = null;
        Library.Visibility = Visibility.Visible;
        DashboardStrip.Visibility = scope == "dashboard" ? Visibility.Visible : Visibility.Collapsed;
        ViewModel.Scope = scope;
        ViewModel.Query = "";
        ViewModel.PageTitle = App.Language.Translate(scope switch
        {
            "dashboard" => "Votre atelier",
            "pinned" => "Épinglés",
            "favorites" => "Favoris",
            "recent" => "Récemment actifs",
            "dirty" => "Git à vérifier",
            "forgotten" => "Projets oubliés",
            "archived" => "Archivés",
            _ => scope.StartsWith("collection:") ? scope[11..] : "Bibliothèque"
        });
        ViewModel.PageSubtitle = App.Language.Translate(scope switch
        {
            "dashboard" => "Retrouvez vos projets et reprenez là où vous en étiez.",
            "forgotten" => "Aucune activité détectée depuis plus de six mois.",
            "dirty" => "Les dépôts qui contiennent des changements non commités.",
            _ => "Scanner, comprendre, organiser, rechercher, lancer."
        });
        App.Language.Apply(Root);
    }
    public async Task AddRootAsync()
    {
        try
        {
            var picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
            var folder = await picker.PickSingleFolderAsync();
            if (folder == null)
                return;
            if (!App.Engine.Settings.Roots.Contains(folder.Path, StringComparer.OrdinalIgnoreCase))
                App.Engine.Settings.Roots.Add(folder.Path);
            await App.Engine.SaveSettingsAsync();
            ViewModel.Status = "Dossier ajouté : " + folder.Path;
        }
        catch (Exception ex) { ViewModel.Status = ex.Message; }
    }
    public async Task ShowProjectAsync(ProjectRecord p, int section = 0)
    {
        var width = Math.Min(960, Root.ActualWidth - 80);
        var details = new ProjectDetailsView(p);
        details.ShowSection(section);
        details.Width = Math.Max(320, width - 64);
        details.Height = Math.Min(570, Root.ActualHeight - 240);
        var dialog = new ContentDialog { Title = p.Name, XamlRoot = Root.XamlRoot, Content = details, CloseButtonText = "Fermer", DefaultButton = ContentDialogButton.Close };
        dialog.Resources["ContentDialogMaxWidth"] = width;
        await dialog.ShowAsync();
        await details.SaveAsync();
    }
    public void OpenEditor(ProjectRecord p)
    {
        var tool = App.Tools.FirstOrDefault(x => x.Name == App.Engine.Settings.PreferredEditor) ?? App.Tools.FirstOrDefault(x => x.Name == "Godot" && p.Technologies.Contains("Godot")) ?? App.Tools.FirstOrDefault(x => x.Kind == "Editor");
        Launch(() => { if (tool == null) App.Launcher.OpenFolder(p); else App.Launcher.OpenTool(p, tool); }, p, tool == null ? "Dossier ouvert" : "Ouvert dans " + tool.Name);
    }
    public async void Launch(Action action, ProjectRecord p, string label)
    {
        try
        {
            action();
            p.LastActivity = DateTimeOffset.UtcNow;
            ProjectEnricher.AddActivity(p, "Ouverture", label);
            await App.Engine.SaveProjectAsync(p);
            ViewModel.Status = label;
        }
        catch (Exception ex) { ViewModel.Status = ex.Message; App.Engine.Log.Write("Launcher", ex.Message); }
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => ViewModel.CancelScan();
    private async void Palette_Click(object sender, RoutedEventArgs e) => await ShowPaletteAsync();
    private async Task ShowPaletteAsync()
    {
        var box = new TextBox { PlaceholderText = "Projet ou commande…" };
        var list = new ListView { MaxHeight = 360 };
        var actions = new List<(string Label, Action Action)> { ("Scanner les dossiers", () => _ = ViewModel.ScanAsync()), ("Ajouter un dossier", () => _ = AddRootAsync()), ("Paramètres", () => ShowPage(new SettingsPage())), ("Projets oubliés", () => ShowLibrary("forgotten")), ("Dépôts Git modifiés", () => ShowLibrary("dirty")), ("Rechercher des projets", () => ShowSearch()) };
        var matches = new List<(string Label, Action Action)>();
        void Refresh()
        {
            matches = actions.Concat(App.Engine.Projects.Select(p => (p.Name + " · " + p.Stack, (Action)(() => OpenEditor(p))))).Select(a => (Item: a, Score: ProjectQuery.FuzzyScore(a.Item1, box.Text))).Where(a => a.Score >= 0).OrderByDescending(a => a.Score).Take(25).Select(a => a.Item).ToList();
            list.ItemsSource = matches.Select(a => a.Label).ToList();
            if (matches.Count > 0)
                list.SelectedIndex = 0;
        }
        box.TextChanged += (_, _) => Refresh();
        Refresh();
        var content = new StackPanel { Spacing = 12, Width = 570 };
        content.Children.Add(box);
        content.Children.Add(list);
        var dialog = new ContentDialog { Title = "Palette de commandes", Content = content, XamlRoot = Root.XamlRoot, PrimaryButtonText = "Ouvrir", CloseButtonText = "Fermer" };
        dialog.Resources["ContentDialogMaxWidth"] = 660d;
        Action? selected = null;
        dialog.PrimaryButtonClick += (_, _) => { if (list.SelectedIndex >= 0) selected = matches[list.SelectedIndex].Action; };
        list.DoubleTapped += (_, _) => { if (list.SelectedIndex >= 0) selected = matches[list.SelectedIndex].Action; dialog.Hide(); };
        dialog.Opened += (_, _) => box.Focus(FocusState.Programmatic);
        await dialog.ShowAsync();
        selected?.Invoke();
    }
    private void Root_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }
    private async void Root_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
            return;
        try
        {
            var items = await e.DataView.GetStorageItemsAsync();
            foreach (var folder in items.OfType<Windows.Storage.StorageFolder>())
                if (!App.Engine.Settings.Roots.Contains(folder.Path, StringComparer.OrdinalIgnoreCase))
                    App.Engine.Settings.Roots.Add(folder.Path);
            await App.Engine.SaveSettingsAsync();
            await ViewModel.ScanAsync();
        }
        catch (Exception ex) { ViewModel.Status = ex.Message; }
    }
}
