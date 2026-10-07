namespace ProjectHub.Core.Models;

public sealed record ProjectCustomization(string Path, bool Favorite, bool Pinned, int PinOrder, bool Archived, string Notes, List<string> Tags, List<string> Collections, string ImagePath, List<string> Screenshots)
{
    public void ApplyTo(ProjectRecord project)
    {
        project.Favorite = Favorite;
        project.Pinned = Pinned;
        project.PinOrder = PinOrder;
        project.Archived = Archived;
        project.Notes = Notes;
        project.Tags = Tags.ToList();
        project.Collections = Collections.ToList();
        project.ImagePath = ImagePath;
        project.Screenshots = Screenshots.ToList();
    }
}
