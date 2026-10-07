namespace ProjectHub.Core.Detection.Detectors;

public sealed class JavaProjectDetector : IProjectDetector
{
    public DetectionResult? Detect(DetectionContext c)
    {
        if (!new[] { "pom.xml", "build.gradle", "build.gradle.kts", "settings.gradle", "settings.gradle.kts", "AndroidManifest.xml" }.Any(c.Has))
            return null;
        var text = c.Read("build.gradle") + c.Read("build.gradle.kts") + c.Read("gradle.properties") + c.Read("pom.xml");
        var tech = new List<string> { c.Has("build.gradle.kts") ? "Kotlin" : "Java" };
        foreach (var (key, label) in new[] { ("fabric", "Fabric"), ("neoforge", "NeoForge"), ("minecraftforge", "Forge"), ("org.bukkit", "Bukkit"), ("papermc", "Paper"), ("com.android", "Android"), ("spring", "Spring") })
            if (text.Contains(key, StringComparison.OrdinalIgnoreCase))
                tech.Add(label);
        if (c.Has("AndroidManifest.xml") && !tech.Contains("Android"))
            tech.Add("Android");
        tech.Add(c.Has("pom.xml") ? "Maven" : "Gradle");
        return new(tech, OwnsChildren: c.Has("settings.gradle") || c.Has("settings.gradle.kts"));
    }
}
