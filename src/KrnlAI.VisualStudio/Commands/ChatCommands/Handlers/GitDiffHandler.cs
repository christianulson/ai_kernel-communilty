using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class GitDiffHandler
{
    public static SlashCommand Create(IGitService git) =>
        new("diff", "Show git diff",
            async (args, ct) =>
            {
                var diff = await git.DiffAsync(ct);
                return GitDiffParser.FormatForChat(diff);
            });
}
