using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class RunCommandHandler
{
    public static SlashCommand Create(ITerminalService terminal, ISolutionContextService context) =>
        new("run", "Run a shell command",
            async (args, ct) =>
            {
                if (string.IsNullOrWhiteSpace(args))
                    return "Usage: /run <command>";

                var dir = context.GetSolutionDirectory() ?? ".";
                var result = await terminal.RunAsync(args, dir, ct);
                return TerminalOutputParser.FormatForChat(result);
            });
}
