using System.Collections.ObjectModel;

namespace KrnlAI.Desktop.App.Services;

public sealed record SlashCommandInfo(string Command, string Description, string Icon);

public sealed class SlashCommandService
{
    public static readonly SlashCommandInfo[] Commands =
    {
        new("/explain", "Explain selected code", "\U0001f4a1"),
        new("/fix", "Fix code issues", "\U0001f527"),
        new("/test", "Generate tests", "\U0001f9ea"),
        new("/refactor", "Refactor code", "\U0001f528"),
        new("/review", "Review code", "\U0001f441\ufe0f"),
        new("/doc", "Generate documentation", "\U0001f4dd"),
        new("/run", "Run terminal command", "\u25b6\ufe0f"),
        new("/build", "Build project", "\U0001f3d7\ufe0f"),
        new("/help", "Show help", "\u2753"),
        new("/clear", "Clear conversation", "\U0001f5d1\ufe0f"),
        new("/sessions", "List sessions", "\U0001f4cb"),
    };

    private readonly List<SlashCommandInfo> _all;
    public ReadOnlyCollection<SlashCommandInfo> All => _all.AsReadOnly();
    private List<SlashCommandInfo> _filtered = new();

    public SlashCommandService()
    {
        _all = new List<SlashCommandInfo>(Commands);
    }

    public IReadOnlyList<SlashCommandInfo> Filter(string input)
    {
        if (!input.StartsWith("/"))
        {
            _filtered = new List<SlashCommandInfo>();
            return _filtered;
        }

        var query = input.TrimStart('/').ToLowerInvariant();
        _filtered = _all
            .Where(c => c.Command.Contains(query) || c.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Command)
            .ToList();

        return _filtered;
    }

    public bool IsSlashCommand(string input)
    {
        return _all.Any(c => input.StartsWith(c.Command));
    }
}
