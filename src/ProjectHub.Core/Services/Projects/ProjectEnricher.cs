using System.Security.Cryptography;
using System.Text;
using ProjectHub.Core.Models;
using ProjectHub.Core.Services.Analytics;
using ProjectHub.Core.Services.Git;
using ProjectHub.Core.Services.Launching;
using ProjectHub.Core.Utilities;
namespace ProjectHub.Core.Services.Projects;

public sealed class ProjectEnricher(StatisticsAnalyzer statistics, GitService git)
{
    public async Task EnrichAsync(ProjectRecord project, HubSettings settings, CancellationToken token, bool full = true)
    {
        project.Missing = !Directory.Exists(project.Path);
        if (project.Missing)
            return;
        var previousCommit = project.Git.Commits.FirstOrDefault()?.Hash;
        project.Git = await git.ReadAsync(project.Path, settings.GitExecutable, token);
        if (previousCommit != null && project.Git.Commits.FirstOrDefault()?.Hash is string hash && hash != previousCommit)
            AddActivity(project, "Git", "Nouveau commit : " + project.Git.Commits[0].Subject);
        if (full)
        {
            var (stats, lastWrite) = await statistics.AnalyzeAsync(project.Path, settings, token, project.IndependentSubprojects);
            project.Statistics = stats;
            if (lastWrite > project.LastActivity)
                project.LastActivity = lastWrite;
            var readme = Directory.EnumerateFiles(project.Path).FirstOrDefault(x => Path.GetFileName(x).Equals("README.md", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(x).Equals("README", StringComparison.OrdinalIgnoreCase));
            project.Readme = readme == null ? "" : SafeFileSystem.ReadSmall(readme);
            if (project.Description.Length == 0 && project.Readme.Length > 0)
            {
                var paragraph = project.Readme.Split('\n').Select(x => x.Trim()).FirstOrDefault(x => x.Length > 20 && !x.StartsWith('#') && !x.StartsWith('!') && !x.StartsWith('['));
                project.Description = paragraph == null ? "" : paragraph[..Math.Min(180, paragraph.Length)];
            }
            var manifestText = string.Join("\n", Directory.EnumerateFiles(project.Path).Where(x => new[] { "package.json", "Cargo.toml", "project.godot", "pyproject.toml", "CMakeLists.txt" }.Contains(Path.GetFileName(x))).Order().Select(x => SafeFileSystem.ReadSmall(x)));
            project.Fingerprint = manifestText.Length == 0 ? "" : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(manifestText)));
            project.SignatureBytes = Encoding.UTF8.GetByteCount(manifestText);
            project.Commands = CommandDetector.Detect(project.Path, project.Technologies);
            foreach (var component in project.Components)
                project.Commands.AddRange(CommandDetector.Detect(component.Path, component.Technologies).Select(x => x with { Name = component.Name + " / " + x.Name }));
            project.AutoTags = InferTags(project.Technologies);
            if (project.ImagePath.Length == 0)
                foreach (var name in new[] { "icon.png", "logo.png", "favicon.png", "Assets/icon.png" })
                {
                    var p = Path.Combine(project.Path, name);
                    if (File.Exists(p))
                    {
                        project.ImagePath = p;
                        break;
                    }
                }
        }
        var latestCommit = project.Git.Commits.FirstOrDefault()?.Date ?? default;
        if (latestCommit > project.LastActivity)
            project.LastActivity = latestCommit;
    }
    public static void AddActivity(ProjectRecord p, string kind, string text)
    {
        lock (p)
        {
            p.Activity.Insert(0, new(DateTimeOffset.UtcNow, kind, text));
            if (p.Activity.Count > 250)
                p.Activity.RemoveRange(250, p.Activity.Count - 250);
        }
    }
    private static List<string> InferTags(List<string> technologies)
    {
        var tags = new List<string>();
        if (technologies.Intersect(["Godot", "Unity", "Unreal Engine", "Roblox", "Bevy"]).Any())
            tags.Add("Game");
        if (technologies.Intersect(["React", "Vue", "Svelte", "Next.js", "Express", "ASP.NET Core", "Django", "FastAPI"]).Any())
            tags.Add("Web");
        if (technologies.Intersect(["Tauri", "Electron", "WinUI 3", "WPF", "Avalonia"]).Any())
            tags.Add("Desktop");
        if (technologies.Contains("Android"))
            tags.Add("Mobile");
        if (technologies.Intersect(["Fabric", "Forge", "NeoForge", "Bukkit", "Paper"]).Any())
            tags.Add("Minecraft");
        if (technologies.Intersect(["PyTorch", "TensorFlow"]).Any())
            tags.Add("AI");
        return tags;
    }
}
