using System.Diagnostics;
using ProjectHub.Core.Models;
namespace ProjectHub.Core.Services.Git;

public sealed class GitService
{
    public async Task<GitSnapshot> ReadAsync(string path, string executable, CancellationToken token)
    {
        var result = new GitSnapshot();
        if (!Directory.Exists(System.IO.Path.Combine(path, ".git")) && !File.Exists(System.IO.Path.Combine(path, ".git")))
            return result;
        result.IsRepository = true;
        try
        {
            var status = await Run(path, executable, ["status", "--porcelain=v2", "--branch", "-z"], token);
            var records = status.Split('\0', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < records.Length; i++)
            {
                var row = records[i];
                if (row.StartsWith("# branch.head "))
                    result.Branch = row[14..].Trim();
                else if (row.StartsWith("# branch.ab "))
                {
                    var fields = row.Split(' ');
                    int.TryParse(fields.ElementAtOrDefault(2)?.TrimStart('+'), out var ahead);
                    int.TryParse(fields.ElementAtOrDefault(3)?.TrimStart('-'), out var behind);
                    result.Ahead = ahead;
                    result.Behind = behind;
                }
                else if (row.StartsWith("? "))
                    result.ChangedFiles.Add(new("??", row[2..]));
                else if (row.StartsWith("1 ") || row.StartsWith("2 ") || row.StartsWith("u "))
                {
                    var fields = row.Split(' ', row[0] == '1' ? 9 : row[0] == '2' ? 10 : 11);
                    result.ChangedFiles.Add(new(fields[1], fields[^1]));
                    if (row[0] == '2')
                        i++;
                }
            }
            result.Remote = (await Run(path, executable, ["config", "--get", "remote.origin.url"], token, allowFailure: true)).Trim();
            if (Uri.TryCreate(result.Remote, UriKind.Absolute, out var remoteUri) && remoteUri.Scheme is "http" or "https" && remoteUri.UserInfo.Length > 0)
                result.Remote = new UriBuilder(remoteUri) { UserName = "", Password = "" }.Uri.ToString();
            var log = await Run(path, executable, ["log", "-20", "--format=%H%x1f%an%x1f%aI%x1f%s"], token, allowFailure: true);
            foreach (var line in log.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var f = line.Split('\x1f');
                if (f.Length == 4 && DateTimeOffset.TryParse(f[2], out var date))
                    result.Commits.Add(new(f[0], f[1], date, f[3]));
            }
            result.BranchCount = (await Run(path, executable, ["for-each-ref", "--format=%(refname)", "refs/heads"], token)).Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
        }
        catch (Exception e) when (e is not OperationCanceledException || !token.IsCancellationRequested) { result.Error = e.Message; }
        return result;
    }
    private static async Task<string> Run(string path, string executable, string[] args, CancellationToken token, bool allowFailure = false)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(12));
        var start = new ProcessStartInfo(executable) { WorkingDirectory = path, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = System.Text.Encoding.UTF8, StandardErrorEncoding = System.Text.Encoding.UTF8 };
        foreach (var arg in new[] { "--no-optional-locks", "-c", "core.fsmonitor=false", "-c", "core.hooksPath=NUL", "-c", "core.untrackedCache=false" }.Concat(args))
            start.ArgumentList.Add(arg);
        start.Environment["GIT_TERMINAL_PROMPT"] = "0";
        using var process = Process.Start(start) ?? throw new IOException("Impossible de démarrer Git");
        try
        {
            var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var error = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            var value = await output;
            var failure = await error;
            if (process.ExitCode != 0 && !allowFailure)
                throw new IOException(failure.Trim());
            return value;
        }
        finally { if (!process.HasExited) process.Kill(true); }
    }
}
