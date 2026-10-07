using ProjectHub.Core.Utilities;
namespace ProjectHub.Core.Detection;

public sealed class DetectionContext(string path, string[] files)
{
    public string Path { get; } = path;
    public string[] Files { get; } = files;
    public bool Has(string name) => Files.Any(x => System.IO.Path.GetFileName(x).Equals(name, StringComparison.OrdinalIgnoreCase));
    public bool Extension(string extension) => Files.Any(x => x.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
    public string Read(string name) => SafeFileSystem.ReadSmall(System.IO.Path.Combine(Path, name));
}
