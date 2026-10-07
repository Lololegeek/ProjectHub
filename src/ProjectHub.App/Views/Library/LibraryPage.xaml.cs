using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ProjectHub.App.ViewModels;
using ProjectHub.Core.Models;
namespace ProjectHub.App.Views.Library;

public sealed partial class LibraryPage : UserControl
{
    private LibraryViewModel VM => App.Main.ViewModel;
    public LibraryPage()
    {
        InitializeComponent();
        Loaded += (_, _) => { DataContext = VM; VM.VisibleProjects.CollectionChanged += (_, _) => UpdateEmpty(); UpdateEmpty(); ModeBox.SelectedIndex = App.Engine.Settings.ViewMode switch { "List" => 1, "Compact" => 2, _ => 0 }; };
    }
    private void UpdateEmpty()
    {
        Empty.Visibility = VM.VisibleProjects.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyHeading.Text = App.Engine.Projects.Count == 0 ? "Votre bibliothèque commence ici" : "Aucun projet trouvé";
        EmptyText.Text = App.Engine.Projects.Count == 0 ? "Ajoutez un dossier racine. ProjectHub trouvera les projets qu’il contient." : "Aucun projet ne correspond à ces filtres.";
    }
    public void FocusSearch()
    {
        SearchBox.Focus(FocusState.Programmatic);
        SearchBox.SelectAll();
    }
    public void ApplyPreferences() => ModeBox.SelectedIndex = App.Engine.Settings.ViewMode switch { "List" => 1, "Compact" => 2, _ => 0 };
    private async void AddRoot_Click(object sender, RoutedEventArgs e) => await App.Main.AddRootAsync();
    private async void Scan_Click(object sender, RoutedEventArgs e) => await VM.ScanAsync();
    private async void Project_Click(object sender, ItemClickEventArgs e) => await App.Main.ShowProjectAsync((ProjectRecord)e.ClickedItem);
    private async void Favorite_Click(object sender, RoutedEventArgs e)
    {
        var p = (ProjectRecord)((Button)sender).Tag;
        p.Favorite = !p.Favorite;
        await App.Engine.SaveProjectAsync(p);
    }
    private void Sort_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (App.Main != null && SortBox.SelectedItem is string s)
            VM.Sort = s;
    }
    private async void Mode_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (Cards == null)
            return;
        Cards.Visibility = ModeBox.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        Rows.Visibility = ModeBox.SelectedIndex == 0 ? Visibility.Collapsed : Visibility.Visible;
        Rows.FontSize = ModeBox.SelectedIndex == 2 ? 12 : 14;
        Rows.ItemContainerStyle = new Style(typeof(ListViewItem)) { Setters = { new Setter(ListViewItem.MinHeightProperty, ModeBox.SelectedIndex == 2 ? 32d : 48d), new Setter(ListViewItem.PaddingProperty, new Thickness(0)), new Setter(ListViewItem.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch), new Setter(ListViewItem.HorizontalAlignmentProperty, HorizontalAlignment.Stretch) } };
        if (App.Main != null)
        {
            App.Engine.Settings.ViewMode = ModeBox.SelectedIndex switch
            {
                1 => "List",
                2 => "Compact",
                _ => "Cards"
            };
            await App.Engine.Repository.SaveSettingsAsync(App.Engine.Settings);
            VM.VisibleProjects.Clear();
            VM.Refresh();
        }
    }
    private static ProjectRecord Project(object s) => (ProjectRecord)((MenuFlyoutItem)s).Tag;
    private async void DetailsMenu_Click(object s, RoutedEventArgs e) => await App.Main.ShowProjectAsync(Project(s));
    private async void RunMenu_Click(object s, RoutedEventArgs e) => await App.Main.ShowProjectAsync(Project(s), 6);
    private void EditorMenu_Click(object s, RoutedEventArgs e) => App.Main.OpenEditor(Project(s));
    private void TerminalMenu_Click(object s, RoutedEventArgs e) => App.Main.Launch(() => App.Launcher.OpenTerminal(Project(s)), Project(s), "Terminal ouvert");
    private void FolderMenu_Click(object s, RoutedEventArgs e) => App.Main.Launch(() => App.Launcher.OpenFolder(Project(s)), Project(s), "Dossier ouvert");
    private void GitHubMenu_Click(object s, RoutedEventArgs e) => App.Main.Launch(() => App.Launcher.OpenGitHub(Project(s)), Project(s), "GitHub ouvert");
    private async void FavoriteMenu_Click(object s, RoutedEventArgs e)
    {
        var p = Project(s);
        p.Favorite = !p.Favorite;
        await App.Engine.SaveProjectAsync(p);
    }
    private async void PinMenu_Click(object s, RoutedEventArgs e)
    {
        var p = Project(s);
        p.Pinned = !p.Pinned;
        p.PinOrder = App.Engine.Projects.Count(x => x.Pinned);
        await App.Engine.SaveProjectAsync(p);
    }
    private async void RescanMenu_Click(object s, RoutedEventArgs e)
    {
        try
        {
            await App.Engine.RefreshProjectAsync(Project(s));
        }
        catch (Exception ex) { VM.Status = ex.Message; }
    }
}
