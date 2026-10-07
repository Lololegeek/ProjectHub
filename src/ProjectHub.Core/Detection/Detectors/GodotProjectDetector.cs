using System.Text.RegularExpressions;
namespace ProjectHub.Core.Detection.Detectors;

public sealed class GodotProjectDetector : IProjectDetector
{
    public DetectionResult? Detect(DetectionContext c) => c.Has("project.godot") ? new(["Godot", "GDScript"], Regex.Match(c.Read("project.godot"), "config/description=\"([^\"]*)\"").Groups[1].Value, true) : null;
}
