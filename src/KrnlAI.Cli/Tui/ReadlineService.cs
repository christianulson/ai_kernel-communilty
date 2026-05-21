using System.Text;

namespace KrnlAI.Cli.Tui;

public sealed class ReadlineService
{
    private readonly List<string> _history = new();
    private readonly string _historyFilePath;
    private int _historyIndex = -1;
    private string _savedInput = string.Empty;
    private readonly StringBuilder _buffer = new();
    private int _cursorPos;

    public IReadOnlyDictionary<string, string>? SlashCommands { get; set; }

    public ReadlineService(string? historyFilePath = null)
    {
        _historyFilePath = historyFilePath
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".krnlai", "history.txt");
        LoadHistory();
    }

    public async Task<string?> ReadLineAsync(CancellationToken ct = default)
    {
        _buffer.Clear();
        _cursorPos = 0;
        _historyIndex = -1;
        _savedInput = string.Empty;
        var multiLine = false;
        RenderBuffer();

        while (!ct.IsCancellationRequested)
        {
            var key = await ReadKeyAsync(ct);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    if (multiLine)
                    {
                        // In multi-line mode, up arrow moves cursor up (already default)
                        var currentLine = GetCurrentLine();
                        if (_cursorPos > currentLine)
                        {
                            _cursorPos -= currentLine + 1;
                            UpdateCursor();
                        }
                        else if (_cursorPos > 0)
                        {
                            _cursorPos = 0;
                            UpdateCursor();
                        }
                    }
                    else if (_historyIndex < _history.Count - 1)
                    {
                        if (_historyIndex == -1) _savedInput = _buffer.ToString();
                        _historyIndex++;
                        SetBuffer(_history[_historyIndex]);
                    }
                    break;

                case ConsoleKey.DownArrow:
                    if (_historyIndex > -1)
                    {
                        _historyIndex--;
                        SetBuffer(_historyIndex == -1 ? _savedInput : _history[_historyIndex]);
                    }
                    break;

                case ConsoleKey.Enter:
                    if (key.Modifiers == ConsoleModifiers.Control || key.Modifiers == ConsoleModifiers.Alt)
                    {
                        // Ctrl+Enter or Alt+Enter = new line in multi-line mode
                        _buffer.Insert(_cursorPos, '\n');
                        _cursorPos++;
                        multiLine = true;
                        RenderBuffer();
                        RenderMultiLineIndicator();
                        break;
                    }

                    var result = _buffer.ToString();
                    Console.WriteLine();
                    if (multiLine)
                    {
                        Console.WriteLine(new string('-', Console.BufferWidth - 1));
                        Console.ResetColor();
                    }
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        AddToHistory(result);
                    }
                    return result;

                case ConsoleKey.Tab:
                    if (!multiLine)
                        await HandleTabCompleteAsync(ct);
                    else
                    {
                        _buffer.Insert(_cursorPos, '\t');
                        _cursorPos++;
                        RenderBuffer();
                    }
                    break;

                case ConsoleKey.Backspace:
                    if (_cursorPos > 0)
                    {
                        _buffer.Remove(_cursorPos - 1, 1);
                        _cursorPos--;
                        RenderBuffer();
                    }
                    break;

                case ConsoleKey.Delete:
                    if (_cursorPos < _buffer.Length)
                    {
                        _buffer.Remove(_cursorPos, 1);
                        RenderBuffer();
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    if (_cursorPos > 0) { _cursorPos--; UpdateCursor(); }
                    break;

                case ConsoleKey.RightArrow:
                    if (_cursorPos < _buffer.Length) { _cursorPos++; UpdateCursor(); }
                    break;

                case ConsoleKey.Home:
                    _cursorPos = 0;
                    UpdateCursor();
                    break;

                case ConsoleKey.End:
                    _cursorPos = _buffer.Length;
                    UpdateCursor();
                    break;

                default:
                    if (key.KeyChar >= 32 && key.KeyChar < 127)
                    {
                        _buffer.Insert(_cursorPos, key.KeyChar);
                        _cursorPos++;
                        RenderBuffer();
                    }
                    break;
            }
        }

        return null;
    }

    private void SetBuffer(string text)
    {
        _buffer.Clear();
        _buffer.Append(text);
        _cursorPos = _buffer.Length;
        RenderBuffer();
    }

    private void RenderBuffer()
    {
        var left = Console.CursorLeft;
        var top = Console.CursorTop;

        // Clear current line
        Console.SetCursorPosition(0, top);
        Console.Write(new string(' ', Console.BufferWidth - 1));
        Console.SetCursorPosition(0, top);

        // Write prompt prefix and buffer
        Console.Write("> ");
        Console.Write(_buffer.ToString());

        // Position cursor
        Console.SetCursorPosition(2 + _cursorPos, top);
    }

    private void UpdateCursor()
    {
        try
        {
            Console.CursorLeft = 2 + _cursorPos;
        }
        catch
        {
            // Ignore cursor positioning errors
        }
    }

    private static async Task<ConsoleKeyInfo> ReadKeyAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (Console.KeyAvailable)
            {
                return Console.ReadKey(intercept: true);
            }
            await Task.Delay(10, ct);
        }
        ct.ThrowIfCancellationRequested();
        return default;
    }

    private async Task HandleTabCompleteAsync(CancellationToken ct)
    {
        if (SlashCommands == null) return;

        var prefix = _buffer.ToString();
        if (!prefix.StartsWith("/"))
        {
            // Not a slash command, nothing to complete
            return;
        }

        var query = prefix[1..];
        var matches = SlashCommands.Keys
            .Where(c => c.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c)
            .ToList();

        if (matches.Count == 0) return;

        if (matches.Count == 1)
        {
            // Complete to the single match
            SetBuffer("/" + matches[0] + " ");
            return;
        }

        // Multiple matches: find common prefix
        var commonPrefix = FindCommonPrefix(matches);
        if (commonPrefix.Length > query.Length)
        {
            SetBuffer("/" + commonPrefix);
            return;
        }

        // Show all matches
        var top = Console.CursorTop;
        Console.WriteLine();
        foreach (var match in matches)
        {
            var desc = SlashCommands.TryGetValue(match, out var d) ? d : "";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"  /{match,-20}");
            Console.ResetColor();
            Console.WriteLine($" {desc}");
        }
        Console.SetCursorPosition(0, top);
        RenderBuffer();
    }

    private static string FindCommonPrefix(List<string> strings)
    {
        if (strings.Count == 0) return string.Empty;
        if (strings.Count == 1) return strings[0];

        var prefix = strings[0];
        for (var i = 1; i < strings.Count; i++)
        {
            while (!strings[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                prefix = prefix[..^1];
                if (prefix.Length == 0) return string.Empty;
            }
        }
        return prefix;
    }

    private void AddToHistory(string text)
    {
        if (_history.Count > 0 && _history[0] == text) return;
        _history.Insert(0, text);
        if (_history.Count > 100) _history.RemoveAt(_history.Count - 1);
        SaveHistory();
    }

    private void LoadHistory()
    {
        try
        {
            if (File.Exists(_historyFilePath))
            {
                var lines = File.ReadAllLines(_historyFilePath);
                _history.Clear();
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        _history.Add(line);
                }
            }
        }
        catch
        {
            // Ignore load errors
        }
    }

    private int GetCurrentLine()
    {
        var text = _buffer.ToString();
        var beforeCursor = text[..Math.Min(_cursorPos, text.Length)];
        var lastNewline = beforeCursor.LastIndexOf('\n');
        return lastNewline >= 0 ? _cursorPos - lastNewline - 1 : _cursorPos;
    }

    private void RenderMultiLineIndicator()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(" [Ctrl+Enter=new line, Enter=send]");
        Console.ResetColor();
    }

    private void SaveHistory()
    {
        try
        {
            var dir = Path.GetDirectoryName(_historyFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllLines(_historyFilePath, _history.Take(100));
        }
        catch
        {
            // Ignore save errors
        }
    }
}
