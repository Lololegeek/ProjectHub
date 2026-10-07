namespace ProjectHub.Core.Detection;

public interface IProjectDetector
{
    DetectionResult? Detect(DetectionContext context);
}
public sealed record DetectionResult(List<string> Technologies, string Description = "", bool OwnsChildren = false);
