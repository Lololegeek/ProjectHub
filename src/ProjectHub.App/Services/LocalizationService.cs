using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace ProjectHub.App.Services;

public sealed class LocalizationService
{
    private static readonly Dictionary<string, string> English = new(StringComparer.Ordinal)
    {
        ["Votre atelier de développement"] = "Your development workspace",
        ["Vue d’ensemble"] = "Overview", ["Bibliothèque"] = "Library", ["Épinglés"] = "Pinned",
        ["Favoris"] = "Favorites", ["Récents"] = "Recent", ["Git à vérifier"] = "Git needs attention",
        ["Projets oubliés"] = "Forgotten projects", ["Archivés"] = "Archived", ["Collections"] = "Collections",
        ["Statistiques"] = "Analytics", ["Tâches"] = "Tasks", ["Diagnostics"] = "Diagnostics",
        ["Projets indexés"] = "Indexed projects", ["Dépôts à vérifier"] = "Repositories to review",
        ["Espace sur disque"] = "Disk space", ["Annuler le scan"] = "Cancel scan",
        ["Paramètres"] = "Settings", ["Votre bibliothèque, à votre façon."] = "Your library, your way.",
        ["Dossiers et scan"] = "Folders & scanning", ["Apparence"] = "Appearance", ["Éditeurs et Git"] = "Editors & Git",
        ["Fonctionnement"] = "Behavior", ["Configuration"] = "Configuration", ["Dossiers surveillés"] = "Watched folders",
        ["ProjectHub détecte automatiquement les projets dans ces dossiers."] = "ProjectHub automatically finds projects in these folders.",
        ["Ajouter un dossier"] = "Add folder", ["Aucun dossier ajouté. Choisissez un dossier contenant vos projets."] = "No folders added. Choose a folder that contains your projects.",
        ["Surveillance"] = "Monitoring", ["Suivre les modifications"] = "Watch for changes",
        ["Actualiser l’activité et les informations des projets quand leurs fichiers changent."] = "Refresh project activity and details when files change.",
        ["Découvrir les nouveaux projets"] = "Discover new projects", ["Indexer les projets ajoutés aux dossiers surveillés en arrière-plan."] = "Index projects added to watched folders in the background.",
        ["Inclure les dossiers cachés"] = "Include hidden folders", ["Rechercher aussi les projets dans les dossiers cachés."] = "Also search for projects in hidden folders.",
        ["Exclusions et options avancées"] = "Exclusions & advanced options", ["Dossiers exclus"] = "Excluded folders",
        ["Un nom par ligne. Les exclusions s’appliquent à l’intérieur de chaque racine."] = "One name per line. Exclusions apply within each root folder.",
        ["Profondeur maximale"] = "Maximum depth", ["Nombre de niveaux de dossiers parcourus lors de la découverte."] = "Number of folder levels to scan when discovering projects.",
        ["Suivre les liens symboliques"] = "Follow symbolic links", ["Parcourir les dossiers liés en évitant les boucles."] = "Scan linked folders while avoiding loops.",
        ["Interface"] = "Interface", ["Thème"] = "Theme", ["Choisir l’apparence de ProjectHub ou suivre celle de Windows."] = "Choose ProjectHub's appearance or follow Windows.",
        ["Sombre"] = "Dark", ["Clair"] = "Light", ["Système"] = "System", ["Colonnes de la bibliothèque"] = "Library columns",
        ["Choisir les informations affichées dans les modes Liste et Compact."] = "Choose which details appear in List and Compact views.",
        ["État Git"] = "Git status", ["Taille"] = "Size", ["Activité"] = "Activity", ["Chemin"] = "Path",
        ["Éditeur préféré"] = "Preferred editor", ["Utilisé par l’action Ouvrir. Le mode automatique choisit un outil disponible."] = "Used by Open. Automatic picks an available tool.",
        ["Automatique"] = "Automatic", ["Outils disponibles"] = "Available tools", ["Actualiser"] = "Refresh",
        ["Aucun outil détecté. Ajoutez le chemin de votre éditeur ou moteur ci-dessous."] = "No tools found. Add the path to your editor or engine below.",
        ["Configurer un outil"] = "Configure a tool", ["Nom"] = "Name", ["Exécutable"] = "Executable",
        ["Godot, Rider, VS Code…"] = "Godot, Rider, VS Code…", ["Chemin du fichier .exe"] = "Path to the .exe file",
        ["Parcourir…"] = "Browse…", ["Ajouter l’outil"] = "Add tool", ["Git"] = "Git",
        ["Nom de la commande ou chemin complet de l’exécutable."] = "Command name or full path to the executable.",
        ["Rafraîchissement Git"] = "Git refresh interval", ["Intervalle entre les lectures de l’état des dépôts, en secondes."] = "Time between repository status checks, in seconds.",
        ["Accès rapide"] = "Quick access", ["Zone de notification"] = "System tray", ["Garder ProjectHub accessible après la fermeture de sa fenêtre."] = "Keep ProjectHub available after closing its window.",
        ["Raccourci global"] = "Global shortcut", ["Ouvrir la recherche depuis n’importe quelle application."] = "Open search from any application.",
        ["Touche du raccourci"] = "Shortcut key", ["Combinaison Ctrl + Alt + la touche choisie."] = "Ctrl + Alt plus the selected key.",
        ["Performance du scan"] = "Scan performance", ["Analyses simultanées"] = "Concurrent analyses", ["Limiter le nombre de projets analysés en parallèle."] = "Limit the number of projects analyzed at once.",
        ["Budget par projet"] = "Per-project budget", ["Durée maximale de l’analyse, en secondes. 0 signifie illimité."] = "Maximum analysis time, in seconds. 0 means unlimited.",
        ["Exporter la configuration"] = "Export configuration", ["Sauvegarder vos dossiers, préférences, tags, collections et notes dans un fichier JSON."] = "Save your folders, preferences, tags, collections, and notes to a JSON file.",
        ["Exporter…"] = "Export…", ["Importer une configuration"] = "Import configuration", ["Restaurer les préférences et métadonnées d’un fichier ProjectHub."] = "Restore preferences and metadata from a ProjectHub file.",
        ["Importer…"] = "Import…", ["Les caches du scan et les fichiers de vos projets ne sont pas exportés."] = "Scan caches and project files are not exported.",
        ["Tous les paramètres sont enregistrés."] = "All settings are saved.", ["Modifications non enregistrées."] = "Unsaved changes.",
        ["Annuler les modifications"] = "Discard changes", ["Enregistrer"] = "Save", ["Activé"] = "On", ["Désactivé"] = "Off",
        ["Scanner, comprendre, organiser, rechercher, lancer."] = "Scan, understand, organize, search, and launch.",
        ["Rechercher un projet…  rust · git:dirty · tag:game"] = "Search projects…  rust · git:dirty · tag:game",
        ["Activité récente"] = "Recent activity", ["Lignes de code"] = "Lines of code",
        ["Cartes"] = "Cards", ["Liste"] = "List", ["Compact"] = "Compact", ["Votre bibliothèque commence ici"] = "Your library starts here",
        ["Ajoutez un dossier racine. ProjectHub trouvera les projets qu’il contient."] = "Add a root folder. ProjectHub will find the projects inside it.",
        ["Ouvrir"] = "Open", ["Choisir un éditeur"] = "Choose an editor", ["Terminal"] = "Terminal", ["Dossier"] = "Folder",
        ["Général"] = "General", ["Favori"] = "Favorite", ["Épinglé"] = "Pinned", ["Archivé"] = "Archived",
        ["Ordre"] = "Order", ["Tags personnels (séparés par des virgules)"] = "Personal tags (comma-separated)",
        ["Collections (séparées par des virgules)"] = "Collections (comma-separated)", ["Notes personnelles"] = "Personal notes",
        ["Choisir une image"] = "Choose an image", ["Ajouter un screenshot"] = "Add a screenshot", ["Composants"] = "Components",
        ["Fichiers modifiés · index / working tree"] = "Changed files · index / working tree", ["Derniers commits"] = "Recent commits",
        ["Code"] = "Code", ["Commentaires"] = "Comments", ["Vides"] = "Blank", ["README"] = "README",
        ["Stockage"] = "Storage", ["Voir les dossiers régénérables"] = "Review regenerable folders",
        ["Lancer"] = "Run", ["Ouvrir dans l’éditeur"] = "Open in editor", ["Détails du projet"] = "Project details",
        ["Lancer une commande…"] = "Run a command…", ["Ouvrir le dossier"] = "Open folder", ["Favori / retirer"] = "Favorite / remove",
        ["Épingler / retirer"] = "Pin / unpin", ["Réanalyser"] = "Rescan", ["Paramètres enregistrés."] = "Settings saved."
    };

    public string Current { get; private set; } = "fr";
    public bool IsEnglish => Current == "en";

    public void SetLanguage(string language) => Current = language == "en" ? "en" : "fr";
    public string Translate(string value) => IsEnglish ? English.GetValueOrDefault(value, value) : English.FirstOrDefault(x => x.Value == value).Key ?? value;

    public void Apply(DependencyObject root)
    {
        ApplyNode(root);
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            Apply(VisualTreeHelper.GetChild(root, i));
    }

    private void ApplyNode(DependencyObject node)
    {
        switch (node)
        {
            case TextBlock text when text.GetBindingExpression(TextBlock.TextProperty) == null:
                text.Text = Translate(text.Text);
                break;
            case ComboBox combo:
                for (int i = 0; i < combo.Items.Count; i++)
                    if (combo.Items[i] is string item) combo.Items[i] = Translate(item);
                break;
            case ContentControl control when control.Content is string content:
                control.Content = Translate(content);
                break;
            case MenuFlyoutItem item:
                item.Text = Translate(item.Text);
                break;
            case ToggleSwitch toggle:
                toggle.OnContent = Translate(toggle.OnContent?.ToString() ?? "");
                toggle.OffContent = Translate(toggle.OffContent?.ToString() ?? "");
                break;
            case TextBox box:
                box.PlaceholderText = Translate(box.PlaceholderText);
                if (box.Header is string textBoxHeader) box.Header = Translate(textBoxHeader);
                break;
            case ProjectHub.App.Controls.SettingRow row:
                row.Title = Translate(row.Title);
                row.Description = Translate(row.Description);
                break;
            case NumberBox number when number.Header is string numberHeader:
                number.Header = Translate(numberHeader);
                break;
            case Expander expander when expander.Header is string expanderHeader:
                expander.Header = Translate(expanderHeader);
                break;
            case PivotItem pivot when pivot.Header is string pivotHeader:
                pivot.Header = Translate(pivotHeader);
                break;
            case TabViewItem tab when tab.Header is string tabHeader:
                tab.Header = Translate(tabHeader);
                break;
            case ContentDialog dialog:
                dialog.Title = Translate(dialog.Title?.ToString() ?? "");
                dialog.PrimaryButtonText = Translate(dialog.PrimaryButtonText);
                dialog.CloseButtonText = Translate(dialog.CloseButtonText);
                break;
        }
    }
}
