namespace ProjectHub.Core.Detection.Detectors;

public sealed class PythonProjectDetector : IProjectDetector
{
    public DetectionResult? Detect(DetectionContext c)
    {
        if (!new[] { "pyproject.toml", "requirements.txt", "Pipfile", "setup.py" }.Any(c.Has))
            return null;
        var text = c.Read("pyproject.toml") + c.Read("requirements.txt") + c.Read("Pipfile");
        var tech = new List<string> { "Python" };
        foreach (var (key, label) in new[] { ("django", "Django"), ("fastapi", "FastAPI"), ("flask", "Flask"), ("torch", "PyTorch"), ("tensorflow", "TensorFlow") })
            if (text.Contains(key, StringComparison.OrdinalIgnoreCase))
                tech.Add(label);
        return new(tech);
    }
}
