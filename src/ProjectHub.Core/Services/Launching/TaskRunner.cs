using System.Diagnostics;
using System.Text.RegularExpressions;
using ProjectHub.Core.Models;
using ProjectHub.Core.Providers;
namespace ProjectHub.Core.Services.Launching;

public sealed class TaskRunner : IDisposable
{
    public List<RunningTask> Tasks { get; } = [];
    public event Action? Changed;
    public event Action<RunningTask>? Completed;
    public async Task<RunningTask> StartAsync(ProjectRecord project, LaunchProfile profile)
    {
        var executable = ToolProvider.ResolveExecutable(profile.Executable) ?? throw new IOException("Commande introuvable : " + profile.Executable);
        var start = new ProcessStartInfo(executable) { WorkingDirectory = profile.WorkingDirectory, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = System.Text.Encoding.UTF8, StandardErrorEncoding = System.Text.Encoding.UTF8 };
        start.Environment["PYTHONIOENCODING"] = "utf-8";
        start.Environment["DOTNET_CLI_FORCE_UTF8_ENCODING"] = "1";
        if (executable.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) || executable.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
        {
            // Batch files require cmd. Never interpolate shell metacharacters from manifest keys or paths.
            if (profile.Arguments.Append(executable).Any(x => Regex.IsMatch(x, "[\"&|<>^%!\\r\\n]")))
                throw new IOException("Cette commande batch contient des caractères shell non pris en charge. Lancez-la dans votre terminal.");
            start.FileName = Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe";
            start.Arguments = "/d /s /c \"\"" + executable + "\" " + string.Join(' ', profile.Arguments.Select(x => "\"" + x + "\"")) + "\"";
        }
        else
            foreach (var arg in profile.Arguments)
                start.ArgumentList.Add(arg);
        var task = new RunningTask { Name = project.Name + " / " + profile.Name, ProjectId = project.Id };
        task.Process = Process.Start(start) ?? throw new IOException("Impossible de lancer la commande");
        if (Tasks.Count > 50)
            foreach (var old in Tasks.Where(x => x.ExitCode != null).Take(Tasks.Count - 50).ToArray())
            {
                Tasks.Remove(old);
                old.Process?.Dispose();
            }
        Tasks.Add(task);
        Changed?.Invoke();
        _ = Observe(task);
        await Task.CompletedTask;
        return task;
    }
    private async Task Observe(RunningTask task)
    {
        var process = task.Process!;
        async Task Read(StreamReader reader, string prefix)
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                task.Append(prefix + line);
                Changed?.Invoke();
            }
        }
        try
        {
            await Task.WhenAll(Read(process.StandardOutput, ""), Read(process.StandardError, "[stderr] "), process.WaitForExitAsync());
            task.ExitCode = process.ExitCode;
        }
        catch (Exception e) { task.Append(e.Message); task.ExitCode = -1; }
        finally { task.Duration = DateTimeOffset.UtcNow - task.Started; Changed?.Invoke(); Completed?.Invoke(task); }
    }
    public void Dispose()
    {
        foreach (var task in Tasks)
        {
            task.Stop();
            task.Process?.Dispose();
        }
    }
}
