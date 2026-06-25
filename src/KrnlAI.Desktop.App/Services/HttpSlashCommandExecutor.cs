using System.Net.Http;
using System.Net.Http.Json;
using KrnlAI.Desktop.Core.Abstractions;

namespace KrnlAI.Desktop.App.Services;

public sealed class HttpSlashCommandExecutor : ISlashCommandExecutor
{
    private readonly HttpClient _http;

    public HttpSlashCommandExecutor(string baseUrl = "http://localhost:5235")
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/')), Timeout = TimeSpan.FromSeconds(30) };
    }

    public async Task<string> ExecuteAsync(string input, CancellationToken ct = default)
    {
        var (cmd, args) = Parse(input);

        try
        {
            return cmd switch
            {
                "/undo" => await ExecuteUndoAsync(ct).ConfigureAwait(false),
                "/diff" => await ExecuteDiffAsync(ct).ConfigureAwait(false),
                "/commit" => await ExecuteCommitAsync(args, ct).ConfigureAwait(false),
                "/run" => await ExecuteRunAsync(args, ct).ConfigureAwait(false),
                "/test" => await ExecuteTestAsync(args, ct).ConfigureAwait(false),
                "/clear" => "CLEAR_CONVERSATION",
                "/help" => FormatHelp(),
                "/explain" or "/fix" or "/refactor" or "/review" or "/sessions" =>
                    $"{cmd}: This command is only available in the VS Code or VS extension with editor context.",
                _ => throw new InvalidOperationException($"Command not exist: {cmd}")
            };
        }
        catch (Exception ex)
        {
            return $"Error executing {cmd}: {ex.Message}";
        }
    }

    private async Task<string> ExecuteUndoAsync(CancellationToken ct)
    {
        var response = await _http.PostAsync("/api/undo", null, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UndoResponse>(cancellationToken: ct).ConfigureAwait(false);
        return result?.Message ?? "Undo executed";
    }

    private async Task<string> ExecuteDiffAsync(CancellationToken ct)
    {
        var response = await _http.GetAsync("/api/undo/diff", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<DiffResponse>(cancellationToken: ct).ConfigureAwait(false);
        var diff = result?.Diff ?? "No changes";
        return $"```diff\n{diff}\n```";
    }

    private async Task<string> ExecuteCommitAsync(string message, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync("/api/undo/commit",
            new { message = string.IsNullOrWhiteSpace(message) ? null : message }, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CommitResponse>(cancellationToken: ct).ConfigureAwait(false);
        return result?.Message ?? "Commit created";
    }

    private async Task<string> ExecuteRunAsync(string command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command))
            return "Usage: /run <command>";
        var response = await _http.PostAsJsonAsync("/api/commands/run",
            new { command }, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CommandOutputResponse>(cancellationToken: ct).ConfigureAwait(false);
        return $"```\n{result?.Output ?? "No output"}\n```";
    }

    private async Task<string> ExecuteTestAsync(string args, CancellationToken ct)
    {
        var parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var project = parts.Length > 0 ? parts[0] : null;
        var filter = parts.Length > 1 ? string.Join(" ", parts[1..]) : null;
        var response = await _http.PostAsJsonAsync("/api/commands/test",
            new { project, filter }, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CommandOutputResponse>(cancellationToken: ct).ConfigureAwait(false);
        return $"```\n{result?.Output ?? "Tests completed"}\n```";
    }

    private static string FormatHelp()
        => """
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
