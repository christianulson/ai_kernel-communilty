using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class BuildCommandHandler
{
    public static SlashCommand Create(ITerminalService terminal) =>
        new("build", "Build the current solution",
            async (args, ct) =>
            {
                var result = await terminal.BuildSolutionAsync(ct);
                return TerminalOutputParser.FormatForChat(result);
            });
}
