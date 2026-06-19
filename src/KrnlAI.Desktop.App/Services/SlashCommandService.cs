using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;

namespace KrnlAI.Desktop.App.Services;

public sealed record SlashCommandInfo(string Command, string Description, string Icon);

public sealed class SlashCommandHandler
{
    private readonly HttpClient _http;

    public SlashCommandHandler(string baseUrl = "http://localhost:5235")
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/')), Timeout = TimeSpan.FromSeconds(30) };
    }

    public async Task<string> ExecuteAsync(string input)
    {
        var (cmd, args) = Parse(input);

        try
        {
            return cmd switch
            {
                "/undo" => await ExecuteUndoAsync(),
                "/diff" => await ExecuteDiffAsync(),
                "/commit" => await ExecuteCommitAsync(args),
                "/run" => await ExecuteRunAsync(args),
                "/test" => await ExecuteTestAsync(args),
                "/clear" => "CLEAR_CONVERSATION",
                "/help" => FormatHelp(),
                "/explain" or "/fix" or "/refactor" or "/review" or "/sessions" => $"{cmd}: This command is only available in the VS Code or VS extension with editor context.",
                _=> throw new InvalidOperationException($"Command not exist: {cmd}")
            };
        }
        catch (Exception ex)
        {
            return $"Error executing {cmd}: {ex.Message}";
        }
    }

    private async Task<string> ExecuteUndoAsync()
    {
        var response = await _http.PostAsync("/api/undo", null);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UndoResponse>();
        return result?.Message ?? "Undo executed";
    }

    private async Task<string> ExecuteDiffAsync()
    {
        var response = await _http.GetAsync("/api/undo/diff");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<DiffResponse>();
        var diff = result?.Diff ?? "No changes";
        return $"```diff\n{diff}\n```";
    }

    private async Task<string> ExecuteCommitAsync(string message)
    {
        var response = await _http.PostAsJsonAsync("/api/undo/commit",
            new { message = string.IsNullOrWhiteSpace(message) ? null : message });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CommitResponse>();
        return result?.Message ?? "Commit created";
    }

    private async Task<string> ExecuteRunAsync(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return "Usage: /run <command>";
        var response = await _http.PostAsJsonAsync("/api/commands/run",
            new { command });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CommandOutputResponse>();
        return $"```\n{result?.Output ?? "No output"}\n```";
    }

    private async Task<string> ExecuteTestAsync(string args)
    {
        var parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var project = parts.Length > 0 ? parts[0] : null;
        var filter = parts.Length > 1 ? string.Join(" ", parts[1..]) : null;
        var response = await _http.PostAsJsonAsync("/api/commands/test",
            new { project, filter });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CommandOutputResponse>();
        return $"```\n{result?.Output ?? "Tests completed"}\n```";
    }

    private static string FormatHelp()
    {
        return """
  /undo — Undo last action
  /diff — Show last changes diff
  /commit — Generate and create commit
  /run — Run terminal command: /run <cmd>
  /test — Run tests: /test [project] [filter]
  /clear — Clear conversation
  /help — Show this help
  /explain — Explain code (VS Code/VS only)
  /fix — Fix code (VS Code/VS only)
  /refactor — Refactor code (VS Code/VS only)
  /review — Review code (VS Code/VS only)
  /sessions — List sessions (VS Code/VS only)
""";
    }

    public bool IsSlashCommand(string input) => input.TrimStart().StartsWith("/");

    private static (string Command, string Args) Parse(string input)
    {
        var trimmed = input.Trim();
        var spaceIdx = trimmed.IndexOf(' ');
        if (spaceIdx == -1) return (trimmed.ToLowerInvariant(), "");
        var cmd = trimmed[..spaceIdx].ToLowerInvariant();
        var args = trimmed[(spaceIdx + 1)..].Trim();
        return (cmd, args);
    }

    private sealed record UndoResponse(string? Message);
    private sealed record DiffResponse(string? Diff);
    private sealed record CommitResponse(string? Message);
    private sealed record CommandOutputResponse(string? Output);
}

public sealed class SlashCommandService
{
    private static readonly SlashCommandInfo[] AllCommands =
    {
        new("/undo", "Undo last action", "\U0001f519"),
        new("/diff", "Show last changes diff", "\U0001f4cb"),
        new("/commit", "Generate and create commit", "\U0001f4be"),
        new("/run", "Run terminal command", "\u25b6\ufe0f"),
        new("/test", "Run tests", "\U0001f9ea"),
        new("/clear", "Clear conversation", "\U0001f5d1\ufe0f"),
        new("/help", "Show help", "\u2753"),
        new("/explain", "Explain selected code", "\U0001f4a1"),
        new("/fix", "Fix code issues", "\U0001f527"),
        new("/refactor", "Refactor code", "\U0001f528"),
        new("/review", "Review code", "\U0001f441\ufe0f"),
        new("/sessions", "List sessions", "\U0001f4cb"),
    };

    public static readonly SlashCommandInfo[] Commands = AllCommands;

    private readonly List<SlashCommandInfo> _all;
    public ReadOnlyCollection<SlashCommandInfo> All => _all.AsReadOnly();
    private List<SlashCommandInfo> _filtered = [];

    private static readonly HashSet<string> LocalCommands = new(StringComparer.OrdinalIgnoreCase) { "/clear", "/help" };

    public SlashCommandService()
    {
        var isLocal = Environment.GetEnvironmentVariable("KRNL__RUN_MODE")?.Equals("Local", StringComparison.OrdinalIgnoreCase) == true;
        _all = isLocal
            ? [.. Commands.Where(c => LocalCommands.Contains(c.Command))]
            : [.. Commands];
    }

    public IReadOnlyList<SlashCommandInfo> Filter(string input)
    {
        if (!input.StartsWith("/"))
        {
            _filtered = [];
            return _filtered;
        }

        var query = input.TrimStart('/').ToLowerInvariant();
        _filtered = [.. _all
            .Where(c => c.Command.Contains(query) || c.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Command)];

        return _filtered;
    }

    public bool IsSlashCommand(string input)
    {
        return _all.Any(c => input.StartsWith(c.Command));
    }
}
