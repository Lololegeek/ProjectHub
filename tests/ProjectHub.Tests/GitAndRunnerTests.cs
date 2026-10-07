using System.Diagnostics;
using ProjectHub.Core.Models;
using ProjectHub.Core.Services.Git;
using ProjectHub.Core.Services.Launching;
using Xunit;
namespace ProjectHub.Tests;

public sealed class GitAndRunnerTests
{
    private static async Task Git(string path, params string[] args)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = path, CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in args)
            start.ArgumentList.Add(arg);
        using var p = Process.Start(start)!;
        await Task.WhenAll(p.StandardOutput.ReadToEndAsync(), p.StandardError.ReadToEndAsync(), p.WaitForExitAsync());
        Assert.Equal(0, p.ExitCode);
    }
    [Fact]
    public async Task ReadsRealRepositoryWithStagedUntrackedAndCommit()
    {
        using var w = new TestWorkspace();
        await Git(w.Root, "init");
        await Git(w.Root, "config", "user.name", "Test");
        await Git(w.Root, "config", "user.email", "test@example.invalid");
        w.File("initial.txt", "first");
        await Git(w.Root, "add", ".");
        await Git(w.Root, "commit", "-m", "Initial");
        w.File("staged.txt", "staged");
        await Git(w.Root, "add", "staged.txt");
        w.File("untracked.txt", "local");
        await Git(w.Root, "remote", "add", "origin", "git@github.com:test/project.git");
        var g = await new GitService().ReadAsync(w.Root, "git", default);
        Assert.True(g.IsRepository);
        Assert.True(g.Dirty);
        Assert.Equal(2, g.ChangedFiles.Count);
        Assert.Single(g.Commits);
        Assert.Equal("Initial", g.Commits[0].Subject);
        Assert.Equal("https://github.com/test/project", g.WebUrl);
        Assert.NotEmpty(g.Branch);
        Assert.Equal(1, g.BranchCount);
    }
    [Fact]
    public async Task RunnerCapturesOutputErrorAndExitCode()
    {
        using var w = new TestWorkspace();
        using var runner = new TaskRunner();
        var task = await runner.StartAsync(new()
        {
            Id = "test",
            Name = "Test"
        }, new("test", "powershell.exe", ["-NoProfile", "-Command", "[Console]::OutputEncoding=[Text.Encoding]::UTF8; [Console]::Out.WriteLine('hello écriture'); [Console]::Error.WriteLine('oops'); exit 7"], w.Root));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (task.ExitCode == null)
            await Task.Delay(50, timeout.Token);
        Assert.Equal(7, task.ExitCode);
        Assert.Contains("hello écriture", task.Output);
        Assert.Contains("[stderr] oops", task.Output);
        Assert.True(task.Duration > TimeSpan.Zero);
    }
}
