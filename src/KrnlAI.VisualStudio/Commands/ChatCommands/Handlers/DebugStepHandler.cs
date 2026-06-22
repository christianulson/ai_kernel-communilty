using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class DebugStepHandler
{
    public static SlashCommand CreateStepOver(IVsDebugService debug) =>
        new("debug-step-over", "Step over the current line in the debugger",
            async (args, ct) =>
            {
                if (debug.State == DebugState.Stopped)
                    return "Debugger is not running.";
                await debug.StepOverAsync(ct);
                return "⏭️ Step over executed.";
            });

    public static SlashCommand CreateStepInto(IVsDebugService debug) =>
        new("debug-step-into", "Step into the current call in the debugger",
            async (args, ct) =>
            {
                if (debug.State == DebugState.Stopped)
                    return "Debugger is not running.";
                await debug.StepIntoAsync(ct);
                return "⏬ Step into executed.";
            });

    public static SlashCommand CreateContinue(IVsDebugService debug) =>
        new("debug-continue", "Continue execution after a breakpoint",
            async (args, ct) =>
            {
                if (debug.State == DebugState.Stopped)
                    return "Debugger is not running.";
                await debug.ContinueAsync(ct);
                return "▶️ Continue executed.";
            });
}
