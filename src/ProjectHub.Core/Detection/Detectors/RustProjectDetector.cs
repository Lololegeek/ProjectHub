namespace ProjectHub.Core.Detection.Detectors;

public sealed class RustProjectDetector : IProjectDetector
{
    public DetectionResult? Detect(DetectionContext c)
    {
        if (!c.Has("Cargo.toml"))
            return null;
        var text = c.Read("Cargo.toml");
        var tech = new List<string> { "Rust" };
        foreach (var (key, label) in new[] { ("tauri", "Tauri"), ("axum", "Axum"), ("bevy", "Bevy"), ("actix-web", "Actix") })
            if (text.Contains(key))
                tech.Add(label);
        if (text.Contains("[workspace]"))
            tech.Add("Workspace Rust");
        return new(tech, OwnsChildren: text.Contains("[workspace]"));
    }
}
