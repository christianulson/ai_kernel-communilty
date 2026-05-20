using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class GitBranchHandler
{
    public static SlashCommand Create(IGitService git) =>
        new("branch", "List git branches",
            async (args, ct) =>
            {
                var branches = await git.BranchAsync(ct);
                return string.IsNullOrWhiteSpace(branches)
                    ? "No branches found."
                    : $"```\n{branches}\n```";
            });
}
