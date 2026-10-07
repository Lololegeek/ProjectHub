using System.Diagnostics;
using System.Text;
namespace ProjectHub.Core.Services.Launching;

public sealed class RunningTask
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public string ProjectId { get; init; } = "";
    public string Name { get; init; } = "";
    public DateTimeOffset Started { get; } = DateTimeOffset.UtcNow;
    public Process? Process
    {
        get; internal set;
    }
    public int? ExitCode
    {
        get; internal set;
    }
    public TimeSpan Duration
    {
        get; internal set;
    }
    public bool IsRunning => Process != null && ExitCode == null;
    public string Status => IsRunning ? "En cours" : $"Terminé · code {ExitCode} · {Duration.TotalSeconds:0.0}s";
    private readonly StringBuilder output = new();
    private readonly object gate = new();
    public string Output
    {
        get
        {
            lock (gate)
                return output.ToString();
        }
    }
    public void Append(string value)
    {
        lock (gate)
        {
            output.AppendLine(value);
            if (output.Length > 200000)
                output.Remove(0, output.Length - 160000);
        }
    }
    public void Stop()
    {
        try
        {
            if (Process is { HasExited: false })
                Process.Kill(true);
        }
        catch (InvalidOperationException) { }
    }
}
