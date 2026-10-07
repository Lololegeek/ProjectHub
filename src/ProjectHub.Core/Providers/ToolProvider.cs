using ProjectHub.Core.Models;
namespace ProjectHub.Core.Providers;

public sealed record InstalledTool(string Name, string Executable, string Kind);
public sealed class ToolProvider
{
    public List<InstalledTool> Detect(HubSettings settings)
    {
        var tools = new List<InstalledTool>();
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string programs = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var candidates = new Dictionary<string, string[]>
        {
            ["VS Code"] = [Path.Combine(local, "Programs/Microsoft VS Code/Code.exe"), Path.Combine(programs, "Microsoft VS Code/Code.exe")],
            ["Cursor"] = [Path.Combine(local, "Programs/cursor/Cursor.exe")],
            ["Zed"] = [Path.Combine(local, "Programs/Zed/Zed.exe")],
            ["Visual Studio"] = [Path.Combine(programs, "Microsoft Visual Studio/2022/Community/Common7/IDE/devenv.exe"), Path.Combine(programs, "Microsoft Visual Studio/2022/Professional/Common7/IDE/devenv.exe"), Path.Combine(programs, "Microsoft Visual Studio/2022/Enterprise/Common7/IDE/devenv.exe")],
            ["Rider"] = FindExecutables(Path.Combine(programs, "JetBrains"), "rider64.exe", 4),
            ["Godot"] = FindExecutables(Path.Combine(local, "Programs"), "Godot*.exe", 2).Concat(FindExecutables("D:\\Godot", "Godot*.exe", 2)).ToArray(),
            ["Unity"] = FindExecutables(Path.Combine(programs, "Unity/Hub/Editor"), "Unity.exe", 3)
        };
        foreach (var pair in settings.ToolOverrides)
            candidates[pair.Key] = [pair.Value];
        foreach (var (name, paths) in candidates)
        {
            var path = paths.FirstOrDefault(File.Exists);
            if (path != null)
                tools.Add(new(name, path, name is "Godot" or "Unity" ? "Engine" : "Editor"));
        }
        return tools;
    }
    private static string[] FindExecutables(string root, string pattern, int depth)
    {
        var found = new List<string>();
        if (!Directory.Exists(root))
            return [];
        var stack = new Stack<(string, int)>();
        stack.Push((root, 0));
        while (stack.Count > 0)
        {
            var (path, d) = stack.Pop();
            try
            {
                found.AddRange(Directory.GetFiles(path, pattern));
                if (d < depth)
                    foreach (var child in Directory.GetDirectories(path))
                        if (!ProjectHub.Core.Utilities.SafeFileSystem.IsReparse(child))
                            stack.Push((child, d + 1));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
        return found.ToArray();
    }
    public static string? ResolveExecutable(string name)
    {
        if (Path.IsPathFullyQualified(name))
            return File.Exists(name) ? name : null;
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
            foreach (var ext in new[] { "", ".exe", ".cmd", ".bat" })
            {
                try
                {
                    var path = Path.Combine(dir.Trim('"'), name + ext);
                    if (File.Exists(path))
                        return path;
                }
                catch (ArgumentException) { }
            }
        return null;
    }
}
