using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class GitCommitHandler
{
    public static SlashCommand Create(IGitService git) =>
        new("commit", "Stage all changes and commit (requires confirmation)",
            async (args, ct) =>
            {
                if (string.IsNullOrWhiteSpace(args))
                    return "Usage: /commit <message>";

                var status = await git.StatusAsync(ct);
                if (string.IsNullOrWhiteSpace(status))
                    return "No changes to commit.";

                var success = await git.CommitAsync(args, ct);
                return success
                    ? $"✅ Committed: \"{args}\""
                    : "❌ Commit failed. Check git configuration.";
            },
            isVisible: () => true);
}
