using Microsoft.VisualBasic.FileIO;
using ProjectHub.Core.Models;
using ProjectHub.Core.Utilities;
namespace ProjectHub.App.Services;

public static class CleanupService
{
    private static readonly HashSet<string> Allowed = new(["node_modules", "target", "bin", "obj", "build", "dist", ".next", ".nuxt", ".cache", ".gradle", ".godot", "__pycache__"], StringComparer.OrdinalIgnoreCase);
    public static void Recycle(ProjectRecord project, StorageEntry entry)
    {
        var root = SafeFileSystem.Normalize(project.Path);
        var path = SafeFileSystem.Normalize(entry.Path);
        if (!entry.Regenerable || !Allowed.Contains(Path.GetFileName(path)) || !string.Equals(Path.GetDirectoryName(path), root, StringComparison.OrdinalIgnoreCase) || !SafeFileSystem.Within(path, root) || SafeFileSystem.IsReparse(root) || SafeFileSystem.IsReparse(path))
            throw new IOException("Ce dossier ne peut pas être nettoyé par ProjectHub.");
        FileSystem.DeleteDirectory(path, UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
    }
}
