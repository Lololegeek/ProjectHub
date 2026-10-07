using System.Text.Json;
using ProjectHub.Core.Models;
using ProjectHub.Core.Utilities;
namespace ProjectHub.Core.Services.Launching;

public static class CommandDetector
{
    public static List<LaunchProfile> Detect(string path, List<string> technologies)
    {
        var commands = new List<LaunchProfile>();
        if (File.Exists(Path.Combine(path, "package.json")))
        {
            try
            {
                using var doc = JsonDocument.Parse(SafeFileSystem.ReadSmall(Path.Combine(path, "package.json")));
                string manager = File.Exists(Path.Combine(path, "pnpm-lock.yaml")) ? "pnpm" : File.Exists(Path.Combine(path, "yarn.lock")) ? "yarn" : "npm";
                if (doc.RootElement.TryGetProperty("scripts", out var scripts) && scripts.ValueKind == JsonValueKind.Object)
                    foreach (var script in scripts.EnumerateObject().Take(30))
                        if (script.Value.ValueKind == JsonValueKind.String)
                            commands.Add(new(manager + " run " + script.Name, manager, ["run", script.Name], path));
            }
            catch (JsonException) { }
        }
        if (File.Exists(Path.Combine(path, "Cargo.toml")))
            foreach (var cmd in new[] { "run", "build", "test" })
                commands.Add(new("cargo " + cmd, "cargo", [cmd], path));
        if (technologies.Contains(".NET"))
        {
            string[] manifests;
            try
            {
                manifests = Directory.GetFiles(path, "*.*proj");
            }
            catch { manifests = []; }
            if (manifests.Length == 1)
            {
                var manifest = SafeFileSystem.ReadSmall(manifests[0]);
                if (System.Text.RegularExpressions.Regex.IsMatch(manifest, @"<OutputType>\s*(WinExe|Exe)\s*</OutputType>", System.Text.RegularExpressions.RegexOptions.IgnoreCase) || manifest.Contains("Microsoft.NET.Sdk.Web"))
                    commands.Add(new("dotnet run", "dotnet", ["run", "--project", manifests[0]], path));
            }
            foreach (var cmd in new[] { "build", "test" })
                commands.Add(new("dotnet " + cmd, "dotnet", [cmd], path));
        }
        if (technologies.Contains("Python"))
            foreach (var file in new[] { "main.py", "app.py", "manage.py" })
                if (File.Exists(Path.Combine(path, file)))
                    commands.Add(new("python " + file, "python", [file], path));
        if (File.Exists(Path.Combine(path, "CMakeLists.txt")))
        {
            commands.Add(new("CMake configure", "cmake", ["-S", ".", "-B", "build"], path));
            commands.Add(new("CMake build", "cmake", ["--build", "build"], path));
        }
        if (File.Exists(Path.Combine(path, "gradlew.bat")))
            foreach (var cmd in new[] { "build", "test" })
                commands.Add(new("Gradle " + cmd, Path.Combine(path, "gradlew.bat"), [cmd], path));
        return commands;
    }
}
