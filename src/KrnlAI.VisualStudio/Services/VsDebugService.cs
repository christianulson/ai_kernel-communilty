using Microsoft.VisualStudio.Shell;

namespace KrnlAI.VisualStudio.Services;

#pragma warning disable VSTHRD010, VSTHRD109 // Default implementations run on UI thread in production

public sealed class VsDebugService : IVsDebugService
{
    private readonly IVsOperationTracker _debugTracker;
    private readonly Func<CancellationToken, Task<string>> _buildAsync;
    private readonly Action _launchAction;
    private readonly Action _stopAction;
    private readonly Action _stepOverAction;
    private readonly Action _stepIntoAction;
    private readonly Action _continueAction;
    private readonly Func<string, int, bool> _setBreakpointFunc;
    private readonly Func<string, int, bool> _removeBreakpointFunc;
    private DebugState _state;

    public DebugState State => _state;
    public event Action<DebugState>? StateChanged;

    public VsDebugService(
        IVsOperationTracker? debugTracker = null,
        Func<CancellationToken, Task<string>>? buildAsync = null,
        Action? launchAction = null,
        Action? stopAction = null,
        Action? stepOverAction = null,
        Action? stepIntoAction = null,
        Action? continueAction = null,
        Func<string, int, bool>? setBreakpointFunc = null,
        Func<string, int, bool>? removeBreakpointFunc = null,
        DebugState state = DebugState.Stopped)
    {
        _debugTracker = debugTracker ?? new VsOperationTracker();
        _buildAsync = buildAsync ?? DefaultBuildAsync;
        _launchAction = launchAction ?? DefaultLaunchAction;
        _stopAction = stopAction ?? DefaultStopAction;
        _stepOverAction = stepOverAction ?? DefaultStepOverAction;
        _stepIntoAction = stepIntoAction ?? DefaultStepIntoAction;
        _continueAction = continueAction ?? DefaultContinueAction;
        _setBreakpointFunc = setBreakpointFunc ?? DefaultSetBreakpoint;
        _removeBreakpointFunc = removeBreakpointFunc ?? DefaultRemoveBreakpoint;
        _state = state;
    }

    public async Task<string> BuildSolutionAsync(CancellationToken ct = default)
    {
        using var op = _debugTracker.Start("debug.build");
        try
        {
            var result = await _buildAsync(ct);
            op.SetResult(result);
            return result;
        }
        catch (Exception ex)
        {
            op.SetError(ex.Message);
            throw;
        }
    }

    public Task<bool> LaunchProjectAsync(string? projectName = null, CancellationToken ct = default)
    {
        using var op = _debugTracker.Start("debug.launch", projectName);

        if (_state != DebugState.Stopped)
        {
            op.SetError($"Cannot launch: state is {_state}");
            return Task.FromResult(false);
        }

        try
        {
            _launchAction();
            SetState(DebugState.Running);
            op.SetResult("Launched");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            op.SetError(ex.Message);
            return Task.FromResult(false);
        }
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        using var op = _debugTracker.Start("debug.stop");

        if (_state == DebugState.Stopped)
        {
            op.SetResult("Already stopped");
            return Task.CompletedTask;
        }

        try
        {
            _stopAction();
            SetState(DebugState.Stopped);
            op.SetResult("Stopped");
        }
        catch (Exception ex)
        {
            op.SetError(ex.Message);
        }

        return Task.CompletedTask;
    }

    public Task StepOverAsync(CancellationToken ct = default)
    {
        return ExecuteDebugCommandAsync(_stepOverAction, "debug.step_over", "Step over");
    }

    public Task StepIntoAsync(CancellationToken ct = default)
    {
        return ExecuteDebugCommandAsync(_stepIntoAction, "debug.step_into", "Step into");
    }

    public Task ContinueAsync(CancellationToken ct = default)
    {
        return ExecuteDebugCommandAsync(_continueAction, "debug.continue", "Continue");
    }

    public Task<bool> SetBreakpointAsync(string filePath, int lineNumber, CancellationToken ct = default)
    {
        using var op = _debugTracker.Start("debug.breakpoint.set", $"{filePath}:{lineNumber}");
        try
        {
            var result = _setBreakpointFunc(filePath, lineNumber);
            op.SetResult(result ? "Set" : "Failed");
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            op.SetError(ex.Message);
            return Task.FromResult(false);
        }
    }

    public Task<bool> RemoveBreakpointAsync(string filePath, int lineNumber, CancellationToken ct = default)
    {
        using var op = _debugTracker.Start("debug.breakpoint.remove", $"{filePath}:{lineNumber}");
        try
        {
            var result = _removeBreakpointFunc(filePath, lineNumber);
            op.SetResult(result ? "Removed" : "Failed");
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            op.SetError(ex.Message);
            return Task.FromResult(false);
        }
    }

    private Task ExecuteDebugCommandAsync(Action command, string opName, string resultText)
    {
        using var op = _debugTracker.Start(opName);

        if (_state == DebugState.Stopped)
        {
            op.SetError("Debugger is not running");
            return Task.CompletedTask;
        }

        try
        {
            command();
            op.SetResult(resultText);
        }
        catch (Exception ex)
        {
            op.SetError(ex.Message);
        }

        return Task.CompletedTask;
    }

    private void SetState(DebugState newState)
    {
        if (_state == newState) return;
        _state = newState;
        StateChanged?.Invoke(_state);
    }

    private static async Task<string> DefaultBuildAsync(CancellationToken _)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
        if (dte?.Solution is null)
            return "No solution is open.";

        dte.Solution.SolutionBuild.Build(true);
        return "Build completed. Check the VS Output window for details.";
    }

    private static void DefaultLaunchAction()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
        dte?.Debugger.Go(false);
    }

    private static void DefaultStopAction()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
        dte?.Debugger.Stop();
    }

    private static void DefaultStepOverAction()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
        dte?.Debugger.StepOver(false);
    }

    private static void DefaultStepIntoAction()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
        dte?.Debugger.StepInto(false);
    }

    private static void DefaultContinueAction()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
        dte?.Debugger.Go(false);
    }

    private static bool DefaultSetBreakpoint(string filePath, int lineNumber)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
        if (dte is null) return false;

        dte.Debugger.Breakpoints.Add(File: filePath, Line: lineNumber);
        return true;
    }

    private static bool DefaultRemoveBreakpoint(string filePath, int lineNumber)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
        if (dte is null) return false;

        foreach (EnvDTE.Breakpoint bp in dte.Debugger.Breakpoints)
        {
            if (bp.File == filePath && bp.FileLine == lineNumber)
            {
                bp.Delete();
                return true;
            }
        }
        return false;
    }
}
