using KrnlAI.Desktop.Core.Abstractions;

namespace KrnlAI.Desktop.App.Services;

public sealed class LocalSlashCommandExecutor : ISlashCommandExecutor
{
    public Task<string> ExecuteAsync(string input, CancellationToken ct = default)
    {
        var (cmd, _) = Parse(input);

        if (cmd == "/clear") return Task.FromResult("CLEAR_CONVERSATION");
        if (cmd == "/help") return Task.FromResult(FormatHelp());

        return Task.FromResult($"{cmd}: Comando não disponível no modo Local");
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
