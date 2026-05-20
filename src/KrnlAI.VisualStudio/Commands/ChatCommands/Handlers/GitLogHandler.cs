using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class GitLogHandler
{
    public static SlashCommand Create(IGitService git) =>
        new("log", "Show git log (last 10 commits)",
            async (args, ct) =>
            {
                var count = 10;
                if (!string.IsNullOrWhiteSpace(args) && int.TryParse(args, out var n))
                    count = n < 1 ? 1 : (n > 100 ? 100 : n);

                var log = await git.LogAsync(count, ct);
                return string.IsNullOrWhiteSpace(log)
                    ? "No commits found."
                    : $"```\n{log}\n```";
            });
}
