namespace ProjectHub.Core.Detection.Detectors;

public sealed class NativeProjectDetector : IProjectDetector
{
    public DetectionResult? Detect(DetectionContext c) => c.Has("CMakeLists.txt") || c.Extension(".vcxproj") || c.Has("Makefile") ? new(["C++", c.Has("CMakeLists.txt") ? "CMake" : "Native"]) : null;
}
