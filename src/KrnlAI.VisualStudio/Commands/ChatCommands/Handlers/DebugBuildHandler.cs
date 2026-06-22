using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class DebugBuildHandler
{
    public static SlashCommand Create(IVsDebugService debug) =>
        new("debug-build", "Build the active solution",
            async (args, ct) =>
            {
                var result = await debug.BuildSolutionAsync(ct);
                return $"```\n{result}\n```";
            });
}
