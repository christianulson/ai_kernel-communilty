using System.Diagnostics;

namespace KrnlAI.VisualStudio.Services;

public sealed class TerminalService(
    IVsOperationTracker? debugTracker = null) : ITerminalService
{
    private readonly IVsOperationTracker _debugTracker = debugTracker ?? new VsOperationTracker();
    private const int DefaultTimeoutSec = 120;

    public async Task<TerminalResult> RunAsync(string command, string workingDir,
        CancellationToken ct)
    {
        using var op = _debugTracker.Start("terminal.run", command);
        try
        {
            var result = await ExecuteProcessAsync(command, workingDir, ct);
            op.SetResult($"exit:{result.ExitCode}");
            return result;
        }
        catch (Exception ex)
        {
            op.SetError(ex.Message);
            throw;
        }
    }

    public async Task<TerminalResult> BuildSolutionAsync(CancellationToken ct)
    {
        using var op = _debugTracker.Start("terminal.build");
        try
        {
            var result = await ExecuteProcessAsync(
                "dotnet build --no-restore",
                await DetectSolutionDirAsync() ?? ".",
                ct);
            op.SetResult(result.ExitCode == 0 ? "Success" : $"Failed ({result.ExitCode})");
            return result;
        }
        catch (Exception ex)
        {
            op.SetError(ex.Message);
            throw;
        }
    }

    public async Task<TerminalResult> RunTestsAsync(string? filter, CancellationToken ct)
    {
        using var op = _debugTracker.Start("terminal.test", filter);
        try
        {
            var cmd = "dotnet test --no-restore --no-build -v minimal";
            if (!string.IsNullOrWhiteSpace(filter))
                cmd += $" --filter \"{filter}\"";
            var result = await ExecuteProcessAsync(cmd, await DetectSolutionDirAsync() ?? ".", ct);
            op.SetResult(result.ExitCode == 0 ? "Passed" : $"Failed ({result.ExitCode})");
            return result;
        }
        catch (Exception ex)
        {
            op.SetError(ex.Message);
            throw;
        }
    }

    private static async Task<TerminalResult> ExecuteProcessAsync(
        string command, string workingDir, CancellationToken ct)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{command}\"",
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
            };

            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            process.Start();

            var readTask = Task.Run(() =>
            {
                var timeoutCt = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCt.CancelAfter(TimeSpan.FromSeconds(DefaultTimeoutSec));
                var timeoutToken = timeoutCt.Token;

                try
                {
                    while (!process.StandardOutput.EndOfStream && !timeoutToken.IsCancellationRequested)
                    {
                        var line = process.StandardOutput.ReadLine();
                        if (line is not null)
                            outputBuilder.AppendLine(line);
                    }
                    while (!process.StandardError.EndOfStream && !timeoutToken.IsCancellationRequested)
                    {
                        var line = process.StandardError.ReadLine();
                        if (line is not null)
                            errorBuilder.AppendLine(line);
                    }
                }
                catch (OperationCanceledException ex)
                {
                    KrnlLogger.Write(ex);
                }
            }, ct);

            await readTask;

            using var exitCts = new CancellationTokenSource(5000);
            try
            {
                process.WaitForExit(5000);
            }
            catch (Exception ex)
            {
                KrnlLogger.Write(ex);
            }

            if (!process.HasExited)
            {
                try { process.Kill(); }
                catch (Exception ex)
                {
                    KrnlLogger.Write(ex);
                }
                return new TerminalResult(-1, outputBuilder.ToString(),
                    "Command timed out after " + DefaultTimeoutSec + " seconds.");
            }

            return new TerminalResult(
                process.ExitCode,
                outputBuilder.ToString(),
                errorBuilder.ToString());
        }
        catch (Exception ex)
        {
            return new TerminalResult(-1, "", ex.Message);
        }
    }

    private static async Task<string?> DetectSolutionDirAsync()
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
