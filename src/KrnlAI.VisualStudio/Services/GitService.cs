using System.Diagnostics;
using System.Text;

namespace KrnlAI.VisualStudio.Services;

public sealed class GitService(
    IVsOperationTracker? debugTracker = null) : IGitService
{
    private readonly IVsOperationTracker _debugTracker = debugTracker ?? new VsOperationTracker();
    private const int DefaultTimeoutSec = 60;

    public async Task<string> StatusAsync(CancellationToken ct)
    {
        using var op = _debugTracker.Start("git.status");
        var result = await RunGitAsync("status --short", ct);
        op.SetResult(result.Contains("Error:") ? "Failed" : "OK");
        return result;
    }

    public async Task<string> DiffAsync(CancellationToken ct)
    {
        using var op = _debugTracker.Start("git.diff");
        var result = await RunGitAsync("diff", ct);
        op.SetResult(result.Contains("Error:") ? "Failed" : "OK");
        return result;
    }

    public async Task<string> LogAsync(int count, CancellationToken ct)
    {
        using var op = _debugTracker.Start("git.log", $"count:{count}");
        var result = await RunGitAsync($"log --oneline -{count}", ct);
        op.SetResult(result.Contains("Error:") ? "Failed" : "OK");
        return result;
    }

    public async Task<string> BranchAsync(CancellationToken ct)
    {
        using var op = _debugTracker.Start("git.branch");
        var result = await RunGitAsync("branch -a", ct);
        op.SetResult(result.Contains("Error:") ? "Failed" : "OK");
        return result;
    }

    public async Task<bool> CommitAsync(string message, CancellationToken ct)
    {
        using var op = _debugTracker.Start("git.commit", message);
        var addResult = await RunGitAsync("add -A", ct);
        if (addResult.Contains("fatal:"))
        {
            op.SetError($"Add failed: {addResult}");
            return false;
        }

        var commitResult = await RunGitAsync($"commit -m \"{EscapeMessage(message)}\"", ct);
        var success = !commitResult.Contains("fatal:");
        if (success) op.SetResult("Committed");
        else op.SetError(commitResult);
        return success;
    }

    public async Task<string> ReviewPullRequestAsync(int prNumber, CancellationToken ct)
    {
        using var op = _debugTracker.Start("git.review_pr", $"#{prNumber}");
        var diffUrl = $"https://github.com/pulls/{prNumber}.diff";
        try
        {
            using var http = new HttpClient();
            var response = await http.GetAsync(diffUrl, ct);
            var diff = await response.Content.ReadAsStringAsync();
            var truncated = TerminalOutputParser.Truncate(diff, 5000);
            op.SetResult($"PR #{prNumber} fetched");
            return $"PR #{prNumber} diff:\n\n```diff\n{truncated}\n```";
        }
        catch (Exception ex)
        {
            op.SetError(ex.Message);
            return $"Could not fetch PR #{prNumber}: {ex.Message}";
        }
    }

    private async Task<string> RunGitAsync(string args, CancellationToken ct)
    {
        try
        {
            var dir = await DetectGitDirAsync() ?? ".";
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = args,
                WorkingDirectory = dir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            var output = new StringBuilder();
            process.Start();

            var readTask = Task.Run(() =>
            {
                var timeoutCt = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCt.CancelAfter(TimeSpan.FromSeconds(DefaultTimeoutSec));

                while (!process.StandardOutput.EndOfStream)
                {
                    var line = process.StandardOutput.ReadLine();
                    if (line is not null) output.AppendLine(line);
                }
                while (!process.StandardError.EndOfStream)
                {
                    var line = process.StandardError.ReadLine();
                    if (line is not null) output.AppendLine(line);
                }
            }, ct);

            await readTask;
            try
            {
                process.WaitForExit(5000);
            }
            catch (Exception ex)
            {
                KrnlLogger.Write(ex);
            }

            return output.ToString().Trim();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private static string EscapeMessage(string message)
    {
        return message.Replace("\"", "\\\"");
    }

    private static async Task<string?> DetectGitDirAsync()
    {
        try
        {
            await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory
                .SwitchToMainThreadAsync();
            var dte = Microsoft.VisualStudio.Shell.Package
                .GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            var solution = dte?.Solution;
            if (solution?.FullName is string path && !string.IsNullOrEmpty(path))
                return System.IO.Path.GetDirectoryName(path);
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            try { return System.IO.Directory.GetCurrentDirectory(); }
            catch (Exception innerEx)
            {
                KrnlLogger.Write(innerEx);
                return null;
            }
        }
        return null;
    }
}
