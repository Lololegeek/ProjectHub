using System.Diagnostics;
using ProjectHub.Core.Models;
using ProjectHub.Core.Providers;
namespace ProjectHub.Core.Services.Launching;

public sealed class LauncherService
{
    public void OpenFolder(ProjectRecord p) => Process.Start(new ProcessStartInfo(p.Path) { UseShellExecute = true });
    public void OpenTerminal(ProjectRecord p)
    {
        var wt = ToolProvider.ResolveExecutable("wt.exe");
        var start = wt != null ? new ProcessStartInfo(wt) : new ProcessStartInfo("powershell.exe");
        start.UseShellExecute = true;
        start.WorkingDirectory = p.Path;
        if (wt != null)
        {
            start.ArgumentList.Add("-d");
            start.ArgumentList.Add(p.Path);
        }
        Process.Start(start);
    }
    public void OpenTool(ProjectRecord p, InstalledTool tool)
    {
        var start = new ProcessStartInfo(tool.Executable) { WorkingDirectory = p.Path, UseShellExecute = true };
        if (tool.Name == "Godot")
        {
            start.ArgumentList.Add("--editor");
            start.ArgumentList.Add("--path");
        }
        if (tool.Name == "Unity")
            start.ArgumentList.Add("-projectPath");
        if (tool.Name == "Visual Studio")
        {
            var solution = Directory.EnumerateFiles(p.Path).FirstOrDefault(x => x.EndsWith(".sln") || x.EndsWith(".slnx"));
            start.ArgumentList.Add(solution ?? p.Path);
        }
        else
            start.ArgumentList.Add(p.Path);
        Process.Start(start);
    }
    public void OpenFile(string path, int line, InstalledTool? tool)
    {
        if (tool == null)
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            return;
        }
        var start = new ProcessStartInfo(tool.Executable) { UseShellExecute = true };
        if (tool.Name is "VS Code" or "Cursor")
        {
            start.ArgumentList.Add("--goto");
            start.ArgumentList.Add(path + ":" + line);
        }
        else
            start.ArgumentList.Add(path);
        Process.Start(start);
    }
    public void OpenGitHub(ProjectRecord p)
    {
        if (p.Git.WebUrl.Length > 0)
            Process.Start(new ProcessStartInfo(p.Git.WebUrl) { UseShellExecute = true });
    }
}
