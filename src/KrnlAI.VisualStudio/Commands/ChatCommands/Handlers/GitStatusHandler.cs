using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class GitStatusHandler
{
    public static SlashCommand Create(IGitService git) =>
        new("status", "Show git status",
            async (args, ct) =>
            {
                var status = await git.StatusAsync(ct);
                return string.IsNullOrWhiteSpace(status)
                    ? "✅ Working tree clean."
                    : $"```\n{status}\n```";
            });
}
