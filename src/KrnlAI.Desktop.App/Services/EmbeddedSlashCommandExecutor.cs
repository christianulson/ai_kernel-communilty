using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Embedded;

namespace KrnlAI.Desktop.App.Services;

public sealed class EmbeddedSlashCommandExecutor : ISlashCommandExecutor
{
    private readonly EmbeddedKrnlAI _kernel;

    public EmbeddedSlashCommandExecutor(EmbeddedKrnlAI kernel)
    {
        _kernel = kernel;
    }

    public async Task<string> ExecuteAsync(string input, CancellationToken ct = default)
    {
        var (cmd, _) = Parse(input);

        if (cmd == "/clear") return "CLEAR_CONVERSATION";
        if (cmd == "/help") return FormatHelp();

        var result = await _kernel.RunAsync(input, ct);
        return result.Narration ?? result.Error ?? "Executed";
    }

    private static (string Command, string Args) Parse(string input)
    {
        var trimmed = input.Trim();
        var spaceIdx = trimmed.IndexOf(' ');
        if (spaceIdx == -1) return (trimmed.ToLowerInvariant(), "");
        var cmd = trimmed[..spaceIdx].ToLowerInvariant();
        var args = trimmed[(spaceIdx + 1)..].Trim();
        return (cmd, args);
    }

    private static string FormatHelp()
        => """
  /clear — Clear conversation
  /help — Show this help
""";
}
