using Microsoft.UI.Xaml.Controls;
using ProjectHub.App.Controls;
using ProjectHub.Core.Models;
namespace ProjectHub.App.Views.Collections;

public sealed class CollectionsPage : UserControl
{
    public CollectionsPage()
    {
        var panel = PageElements.Column();
        Content = PageElements.Scroll(panel);
        panel.Children.Add(PageElements.Heading("Collections"));
        panel.Children.Add(PageElements.Text("Une collection vide de filtre se remplit depuis les détails des projets. Un filtre crée une collection dynamique."));
        var name = new TextBox { Header = "Nom", PlaceholderText = "Jeux Godot" };
        var query = new TextBox { Header = "Filtre dynamique (optionnel)", PlaceholderText = "framework:godot modified:<30d" };
        panel.Children.Add(name);
        panel.Children.Add(query);
        panel.Children.Add(PageElements.Button("Créer la collection", async () => { if (string.IsNullOrWhiteSpace(name.Text)) return; App.Engine.Settings.Collections.RemoveAll(x => x.Name == name.Text.Trim()); App.Engine.Settings.Collections.Add(new(name.Text.Trim(), query.Text.Trim())); await App.Engine.SaveSettingsAsync(); App.Main.ShowPage(new CollectionsPage()); }));
        var builtins = new[] { new CollectionDefinition("Actifs cette semaine", "modified:<7d"), new CollectionDefinition("Projets Rust", "language:rust"), new CollectionDefinition("Jeux Godot", "framework:godot"), new CollectionDefinition("Plus de 1 Go", "size:>1gb"), new CollectionDefinition("Git modifié", "git:dirty"), new CollectionDefinition("Sans README", "readme:false") };
        panel.Children.Add(PageElements.Section("Collections intelligentes"));
        foreach (var c in builtins)
            panel.Children.Add(PageElements.Button(c.Name, () => { App.Main.ShowLibrary("all"); App.Main.ViewModel.Query = c.Query; App.Main.ViewModel.PageTitle = c.Name; }));
        panel.Children.Add(PageElements.Section("Vos collections"));
        foreach (var c in App.Engine.Settings.Collections.ToArray())
            panel.Children.Add(PageElements.Row(PageElements.Button(c.Name + (c.Query.Length > 0 ? " · " + c.Query : ""), () => App.Main.ShowLibrary("collection:" + c.Name)), PageElements.Button("Retirer", async () => { App.Engine.Settings.Collections.Remove(c); await App.Engine.SaveSettingsAsync(); App.Main.ShowPage(new CollectionsPage()); })));
    }
}
