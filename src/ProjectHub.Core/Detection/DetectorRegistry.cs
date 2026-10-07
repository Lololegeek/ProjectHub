using ProjectHub.Core.Detection.Detectors;
namespace ProjectHub.Core.Detection;

public sealed class DetectorRegistry
{
    public List<IProjectDetector> Detectors { get; } = [new GameProjectDetector(), new GodotProjectDetector(), new NodeProjectDetector(), new DotNetProjectDetector(), new RustProjectDetector(), new PythonProjectDetector(), new JavaProjectDetector(), new NativeProjectDetector()];
    public DetectionResult? Detect(DetectionContext context)
    {
        var results = Detectors.Select(x => x.Detect(context)).OfType<DetectionResult>().ToList();
        if (results.Count == 0)
            return Directory.Exists(System.IO.Path.Combine(context.Path, ".git")) || context.Has(".git") ? new(["Git"]) : null;
        return new(results.SelectMany(x => x.Technologies).Distinct().ToList(), results.Select(x => x.Description).FirstOrDefault(x => x.Length > 0) ?? "", results.Any(x => x.OwnsChildren));
    }
}
