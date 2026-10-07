namespace ProjectHub.Core.Models;

public sealed record LaunchProfile(string Name, string Executable, List<string> Arguments, string WorkingDirectory);
