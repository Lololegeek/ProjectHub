using ProjectHub.Core.Models;
namespace ProjectHub.Core.Services.Projects;

public static class ProjectIdentity
{
    public static ProjectRecord? FindMoved(ProjectRecord current, IEnumerable<ProjectRecord> known)
    {
        if (current.SignatureBytes < 32 || current.Fingerprint.Length == 0)
            return null;
        var candidates = known.Where(p => p.Id != current.Id && !Directory.Exists(p.Path) && Directory.Exists(Path.GetPathRoot(p.Path)) && p.Fingerprint == current.Fingerprint && p.Name.Equals(current.Name, StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
        return candidates.Length == 1 ? candidates[0] : null;
    }
    public static void PreserveMetadata(ProjectRecord previous, ProjectRecord current)
    {
        new ProjectCustomization(current.Path, previous.Favorite, previous.Pinned, previous.PinOrder, previous.Archived, previous.Notes, previous.Tags, previous.Collections, current.ImagePath, previous.Screenshots).ApplyTo(current);
        if (previous.ImagePath.Length > 0 && !ProjectHub.Core.Utilities.SafeFileSystem.Within(previous.ImagePath, previous.Path))
            current.ImagePath = previous.ImagePath;
        current.FirstDetected = previous.FirstDetected;
        current.Activity = previous.Activity.ToList();
        ProjectEnricher.AddActivity(current, "Déplacement", "Projet retrouvé depuis " + previous.Path);
    }
}
