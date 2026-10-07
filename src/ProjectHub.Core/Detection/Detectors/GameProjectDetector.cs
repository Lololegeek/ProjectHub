namespace ProjectHub.Core.Detection.Detectors;

public sealed class GameProjectDetector : IProjectDetector
{
    public DetectionResult? Detect(DetectionContext c)
    {
        if (c.Has("default.project.json") || c.Extension(".rbxl") || c.Extension(".rbxlx") || c.Extension(".project.json"))
            return new(["Roblox", "Luau", "Rojo"], OwnsChildren: true);
        if (c.Files.Where(x => x.EndsWith(".luau") || x.EndsWith(".lua")).Take(3).Any(x =>
        {
            var text = ProjectHub.Core.Utilities.SafeFileSystem.ReadSmall(x, 16384);
            return text.Contains("game:GetService(") || text.Contains("Instance.new(") || text.Contains("script.Parent");
        }))
            return new(["Roblox", "Luau"], OwnsChildren: true);
        if (Directory.Exists(System.IO.Path.Combine(c.Path, "Assets")) && Directory.Exists(System.IO.Path.Combine(c.Path, "ProjectSettings")))
            return new(["Unity", "C#"], OwnsChildren: true);
        if (c.Extension(".uproject"))
            return new(["Unreal Engine", "C++"], OwnsChildren: true);
        return null;
    }
}
