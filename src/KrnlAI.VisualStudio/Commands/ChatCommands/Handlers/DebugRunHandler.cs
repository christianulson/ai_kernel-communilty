using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class DebugRunHandler
{
    public static SlashCommand Create(IVsDebugService debug) =>
        new("debug-run", "Build and launch the debugger on the active project",
            async (args, ct) =>
            {
                var launched = await debug.LaunchProjectAsync(
                    string.IsNullOrWhiteSpace(args) ? null : args, ct);
                return launched
                    ? "🚀 Debugger launched."
                    : "⚠️ Could not launch debugger (already running or build failed).";
            });
}
