using Spectre.Console;

namespace KrnlAI.Cli.Tui;

public sealed class TuiInputHandler
{
    private readonly Dictionary<string, string> _commands;

    public TuiInputHandler(Dictionary<string, string> commands)
    {
        _commands = new Dictionary<string, string>(commands, StringComparer.OrdinalIgnoreCase);
    }

    public string ReadInput(string prompt = "> ")
    {
        AnsiConsole.Markup($"[bold cyan]{prompt}[/]");
        var input = Console.ReadLine() ?? "";
        return input.Trim();
    }

    public string ReadInputWithAutocomplete(string prompt = "> ")
    {
        AnsiConsole.Markup($"[bold cyan]{prompt}[/]");

        var input = Console.ReadLine() ?? "";

        if (input.StartsWith('/') && input.IndexOf(' ') == -1)
        {
            var match = Autocomplete(input);
            if (match != null && match != input)
            {
                input = match;
            }
        }

        return input.Trim();
    }

    public string? Autocomplete(string partial)
    {
        if (string.IsNullOrWhiteSpace(partial) || !partial.StartsWith('/'))
            return null;

        var matches = _commands.Keys
            .Where(c => c.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c)
            .ToList();

        if (matches.Count == 0) return null;
        if (matches.Count == 1) return matches[0];

        AnsiConsole.MarkupLine("[grey]Comandos disponíveis:[/]");
        foreach (var match in matches)
        {
            var desc = _commands.TryGetValue(match, out var d) ? d : "";
            AnsiConsole.MarkupLine($"  [cyan]{match,-20}[/] [grey]{desc}[/]");
        }
        return null;
    }

    public (string Command, string Args) Parse(string input)
    {
        var trimmed = input.Trim();

        if (!trimmed.StartsWith('/'))
            return ("", input);

        var spaceIdx = trimmed.IndexOf(' ');
        if (spaceIdx == -1)
            return (trimmed.ToLowerInvariant(), "");

        var cmd = trimmed[..spaceIdx].ToLowerInvariant();
        var args = trimmed[(spaceIdx + 1)..].Trim();
        return (cmd, args);
    }

    public Dictionary<string, string> GetCommandDescriptions()
    {
        return new Dictionary<string, string>(_commands);
    }

    public void AddCommand(string command, string description)
    {
        _commands[command] = description;
    }

    public bool IsKnownCommand(string command)
    {
        return _commands.ContainsKey(command.ToLowerInvariant());
    }
}
