using System.Text.Json;
namespace ProjectHub.Core.Detection.Detectors;

public sealed class NodeProjectDetector : IProjectDetector
{
    public DetectionResult? Detect(DetectionContext c)
    {
        if (!c.Has("package.json"))
            return null;
        var technologies = new List<string> { c.Has("tsconfig.json") ? "TypeScript" : "JavaScript", "Node.js" };
        string description = "";
        bool workspace = false;
        try
        {
            using var doc = JsonDocument.Parse(c.Read("package.json"));
            var root = doc.RootElement;
            if (root.TryGetProperty("description", out var d) && d.ValueKind == JsonValueKind.String)
                description = d.GetString() ?? "";
            workspace = root.TryGetProperty("workspaces", out _) || c.Has("pnpm-workspace.yaml");
            foreach (var section in new[] { "dependencies", "devDependencies" })
                if (root.TryGetProperty(section, out var deps) && deps.ValueKind == JsonValueKind.Object)
                    foreach (var (key, label) in new[] { ("next", "Next.js"), ("react", "React"), ("vue", "Vue"), ("svelte", "Svelte"), ("@sveltejs/kit", "SvelteKit"), ("vite", "Vite"), ("electron", "Electron"), ("@tauri-apps/api", "Tauri"), ("express", "Express"), ("@nestjs/core", "NestJS"), ("@angular/core", "Angular"), ("astro", "Astro") })
                        if (deps.TryGetProperty(key, out _))
                            technologies.Add(label);
        }
        catch (JsonException) { description = "Manifeste package.json invalide"; }
        if (Directory.Exists(System.IO.Path.Combine(c.Path, "src-tauri")))
            technologies.Add("Tauri");
        if (workspace)
            technologies.Add("Monorepo");
        return new(technologies.Distinct().ToList(), description, workspace || technologies.Contains("Tauri") || technologies.Contains("Electron"));
    }
}
