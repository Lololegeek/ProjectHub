using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ProjectHub.App.Controls;
using ProjectHub.Core.Models;
using ProjectHub.Core.Services.Analytics;
namespace ProjectHub.App.Views.Analytics;

public sealed class AnalyticsPage : UserControl
{
    public AnalyticsPage()
    {
        var panel = PageElements.Column();
        Content = PageElements.Scroll(panel);
        panel.Children.Add(PageElements.Heading("Statistiques"));
        var projects = App.Engine.Projects.ToArray();
        var size = LibraryAnalytics.SizeLabel(projects);
        long lines = projects.Sum(p => p.Statistics.Lines);
        panel.Children.Add(PageElements.Text($"{projects.Length} projets · {lines:N0} lignes · {size}\n{projects.Count(p => p.LastActivity > DateTimeOffset.UtcNow.AddDays(-7))} actifs cette semaine · {projects.Count(p => p.Favorite)} favoris · {projects.Count(p => p.Git.Dirty)} dépôts modifiés"));
        panel.Children.Add(PageElements.Section("Langages"));
        var languages = projects.SelectMany(p => p.Statistics.Languages).GroupBy(x => x.Language).Select(x => new { Name = x.Key, Lines = x.Sum(l => l.Code) }).OrderByDescending(x => x.Lines).ToArray();
        long total = languages.Sum(x => x.Lines);
        foreach (var language in languages)
        {
            var row = PageElements.Column();
            row.Spacing = 4;
            row.Children.Add(PageElements.Text($"{language.Name} · {language.Lines:N0} lignes de code · {(total == 0 ? 0 : language.Lines * 100.0 / total):0.0}%"));
            row.Children.Add(new ProgressBar { Value = total == 0 ? 0 : language.Lines * 100.0 / total, Height = 6, MaxWidth = 760, HorizontalAlignment = HorizontalAlignment.Stretch });
            panel.Children.Add(row);
        }
        panel.Children.Add(PageElements.Section("Occupation disque"));
        foreach (var p in projects.OrderByDescending(p => p.Statistics.TotalBytes).Take(12))
            panel.Children.Add(PageElements.Button($"{p.Name} · {p.SizeLabel}", () => _ = App.Main.ShowProjectAsync(p)));
        panel.Children.Add(PageElements.Text($"Dossiers potentiellement régénérables : {ProjectRecord.FormatSize(projects.Sum(p => p.Statistics.RecoverableBytes))}. Les projets imbriqués sont comptés avec leur parent."));
        panel.Children.Add(PageElements.Section("Copies probables"));
        var duplicates = DuplicateFinder.Find(projects);
        if (duplicates.Count == 0)
            panel.Children.Add(PageElements.Text("Aucune copie probable détectée."));
        foreach (var d in duplicates.Take(100))
            panel.Children.Add(PageElements.Text($"{d.First.Name} ↔ {d.Second.Name}\n{d.Reason}\n{d.First.Path}\n{d.Second.Path}"));
        panel.Children.Add(PageElements.Section("Premières détections par mois"));
        foreach (var group in projects.GroupBy(p => p.FirstDetected.ToString("yyyy-MM")).OrderByDescending(g => g.Key))
            panel.Children.Add(PageElements.Text($"{group.Key} · {group.Count()} projets détectés"));
        panel.Children.Add(PageElements.Text("Les statistiques reflètent le dernier scan. La classification des commentaires est indicative ; l’historique ne reconstitue pas les changements antérieurs à l’installation."));
    }
}
