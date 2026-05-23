using KrnlAI.Embedded;

namespace KrnlAI.Cli.Services;

public interface ILocalSlashExecutor
{
    Task<string> ExecuteAsync(string input, CancellationToken ct = default);
}

public sealed class LocalSlashHandler : ILocalSlashExecutor
{
    private readonly EmbeddedKrnlAI _kernel;

    public LocalSlashHandler(EmbeddedKrnlAI kernel)
    {
        _kernel = kernel;
    }

    public async Task<string> ExecuteAsync(string input, CancellationToken ct = default)
    {
        var (cmd, _) = Parse(input);

        if (cmd == "/clear") return "CLEAR_CONVERSATION";
        if (cmd == "/help") return "Available commands: /clear, /help";

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
}
