namespace KrnlAI.VisualStudio.Services;

public interface IVsDebugService
{
    DebugState State { get; }
    event Action<DebugState>? StateChanged;

    /// <summary>Build the solution using dotnet CLI.</summary>
    Task<string> BuildSolutionAsync(CancellationToken ct = default);

    /// <summary>Build + launch the debugger on the active/selected project.</summary>
    Task<bool> LaunchProjectAsync(string? projectName = null, CancellationToken ct = default);

    /// <summary>Stop the current debug session.</summary>
    Task StopAsync(CancellationToken ct = default);

    /// <summary>Step over the current line (if debugger is at a breakpoint).</summary>
    Task StepOverAsync(CancellationToken ct = default);

    /// <summary>Step into the current call.</summary>
    Task StepIntoAsync(CancellationToken ct = default);

    /// <summary>Continue execution after a breakpoint.</summary>
    Task ContinueAsync(CancellationToken ct = default);

    /// <summary>Set a breakpoint at the given file and line.</summary>
    Task<bool> SetBreakpointAsync(string filePath, int lineNumber, CancellationToken ct = default);

    /// <summary>Remove a breakpoint at the given file and line.</summary>
    Task<bool> RemoveBreakpointAsync(string filePath, int lineNumber, CancellationToken ct = default);
}
