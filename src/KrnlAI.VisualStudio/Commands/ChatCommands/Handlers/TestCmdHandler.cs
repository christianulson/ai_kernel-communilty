using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class TestCmdHandler
{
    public static SlashCommand Create(ITerminalService terminal) =>
        new("test-cmd", "Run tests",
            async (args, ct) =>
            {
                var filter = !string.IsNullOrWhiteSpace(args) ? args : null;
                var result = await terminal.RunTestsAsync(filter, ct);
                return TerminalOutputParser.FormatForChat(result);
            });
}
