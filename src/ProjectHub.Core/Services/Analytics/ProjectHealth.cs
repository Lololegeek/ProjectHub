using ProjectHub.Core.Models;
namespace ProjectHub.Core.Services.Analytics;

public sealed record HealthResult(int Score, List<string> Reasons);
public static class ProjectHealth
{
    public static HealthResult Evaluate(ProjectRecord p)
    {
        int score = 0;
        var reasons = new List<string>();
        void Add(bool condition, int points, string yes, string no)
        {
            if (condition)
                score += points;
            reasons.Add($"{(condition ? "+" : "−")} {points} : {(condition ? yes : no)}");
        }
        Add(p.Git.IsRepository, 15, "Dépôt Git présent", "Aucun dépôt Git");
        Add(p.Git.IsRepository && !p.Git.Dirty && p.Git.Error.Length == 0, 20, "Dépôt Git propre", "Git non vérifié ou changements en attente");
        Add(p.Readme.Length > 0, 15, "README présent", "README absent");
        Add(p.Statistics.HasLicense, 10, "Licence présente", "Licence absente");
        Add(p.Statistics.HasTests, 15, "Tests détectés", "Aucun test détecté");
        Add(p.Statistics.Todos.Count == 0, 10, "Aucun marqueur TODO/FIXME", "Marqueurs à examiner : " + p.Statistics.Todos.Count);
        Add(p.Statistics.LargeFiles == 0, 5, "Aucun fichier source > 50 Mo", p.Statistics.LargeFiles + " fichiers source > 50 Mo");
        Add(!p.Forgotten, 10, "Activité dans les six derniers mois", "Aucune activité depuis six mois");
        if (p.Statistics.Truncated)
            reasons.Add("Analyse partielle : certains fichiers sont inaccessibles ou une limite a été atteinte.");
        return new(score, reasons);
    }
}
