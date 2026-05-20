namespace KrnlAI.VisualStudio.Services;

public sealed record TerminalResult(
    int ExitCode,
    string Output,
    string Error
);

public interface ITerminalService
{
    Task<TerminalResult> RunAsync(string command, string workingDir,
        CancellationToken ct);

    Task<TerminalResult> BuildSolutionAsync(CancellationToken ct);

    Task<TerminalResult> RunTestsAsync(string? filter, CancellationToken ct);
}
