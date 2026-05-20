using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class GitReviewPrHandler
{
    public static SlashCommand Create(IGitService git, IKernelClientService client) =>
        new("review-pr", "Review a pull request",
            async (args, ct) =>
            {
                if (!int.TryParse(args.Trim(), out var prNumber))
                    return "Usage: /review-pr <PR number>";

                var prDiff = await git.ReviewPullRequestAsync(prNumber, ct);
                if (prDiff.StartsWith("Could not"))
                    return prDiff;

                var prompt = $"Review this pull request diff and summarize potential issues:\n\n{prDiff}";
                var result = await client.RunAgentAsync(prompt, ct: ct);
                return result?.Summary ?? result?.Status ?? "Review unavailable.";
            });
}
