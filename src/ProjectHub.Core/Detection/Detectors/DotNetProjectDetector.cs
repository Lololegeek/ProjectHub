namespace ProjectHub.Core.Detection.Detectors;

public sealed class DotNetProjectDetector : IProjectDetector
{
    public DetectionResult? Detect(DetectionContext c)
    {
        if (!c.Extension(".sln") && !c.Extension(".slnx") && !c.Extension(".csproj") && !c.Extension(".fsproj"))
            return null;
        var text = string.Join("\n", c.Files.Where(x => x.EndsWith("proj")).Select(x => ProjectHub.Core.Utilities.SafeFileSystem.ReadSmall(x)));
        var tech = new List<string> { c.Extension(".fsproj") ? "F#" : "C#", ".NET" };
        if (text.Contains("UseWinUI") || text.Contains("Microsoft.WindowsAppSDK"))
            tech.Add("WinUI 3");
        if (text.Contains("UseWPF"))
            tech.Add("WPF");
        if (text.Contains("Microsoft.NET.Sdk.Web"))
            tech.Add("ASP.NET Core");
        if (text.Contains("Avalonia"))
            tech.Add("Avalonia");
        return new(tech, OwnsChildren: c.Extension(".sln") || c.Extension(".slnx"));
    }
}
