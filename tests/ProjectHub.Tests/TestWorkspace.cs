namespace ProjectHub.Tests;

public sealed class TestWorkspace : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "ProjectHubTests", Guid.NewGuid().ToString("N"));
    public TestWorkspace()
    {
        Directory.CreateDirectory(Root);
    }
    public string File(string relative, string text)
    {
        var path = Path.Combine(Root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllText(path, text);
        return path;
    }
    public void Dispose()
    {
        var resolved = Path.GetFullPath(Root);
        var parent = Path.Combine(Path.GetTempPath(), "ProjectHubTests") + Path.DirectorySeparatorChar;
        if (!resolved.StartsWith(parent, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unexpected fixture path");
        try
        {
            foreach (var file in Directory.EnumerateFiles(resolved, "*", SearchOption.AllDirectories))
                System.IO.File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(resolved, true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }
}
