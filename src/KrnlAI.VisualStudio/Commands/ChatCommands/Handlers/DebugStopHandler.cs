using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class DebugStopHandler
{
    public static SlashCommand Create(IVsDebugService debug) =>
        new("debug-stop", "Stop the current debug session",
            async (args, ct) =>
            {
                var state = debug.State;
                if (state == DebugState.Stopped)
                    return "Debugger is not running.";

                await debug.StopAsync(ct);
                return "🛑 Debugger stopped.";
            });
}
