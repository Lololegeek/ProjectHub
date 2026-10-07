using ProjectHub.Core.Models;
namespace ProjectHub.Core.Utilities;

public static class SafeFileSystem
{
    public static string Normalize(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    public static bool Within(string path, string root)
    {
        var prefix = Path.TrimEndingDirectorySeparator(root);
        if (!Path.EndsInDirectorySeparator(prefix))
            prefix += Path.DirectorySeparatorChar;
        return path.Equals(root, StringComparison.OrdinalIgnoreCase) || path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
    public static bool Skip(DirectoryInfo entry, HubSettings settings) => settings.Exclusions.Contains(entry.Name, StringComparer.OrdinalIgnoreCase) || (!settings.FollowSymlinks && entry.Attributes.HasFlag(FileAttributes.ReparsePoint)) || (!settings.IncludeHidden && entry.Attributes.HasFlag(FileAttributes.Hidden));
    public static string ReadSmall(string path, int limit = 262144)
    {
        if (IsReparse(path))
            return "";
        try
        {
            using var reader = new StreamReader(path);
            var buffer = new char[limit];
            var count = reader.ReadBlock(buffer, 0, buffer.Length);
            return new string(buffer, 0, count);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return ""; }
    }
    public static bool IsReparse(string path)
    {
        try
        {
            return File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
        }
        catch { return true; }
    }
}
