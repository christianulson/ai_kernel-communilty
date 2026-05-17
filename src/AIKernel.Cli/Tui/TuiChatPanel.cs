using Spectre.Console;

namespace AIKernel.Cli.Tui;

public sealed class TuiChatPanel
{
    private readonly List<ChatMessage> _messages = [];
    private const int MaxMessages = 100;

    public void AddMessage(string role, string content, bool isError = false)
    {
        _messages.Add(new ChatMessage(role, content, isError, DateTimeOffset.UtcNow));
        if (_messages.Count > MaxMessages)
            _messages.RemoveAt(0);
    }

    public Panel Render()
    {
        var lines = new List<string>();

        foreach (var msg in _messages)
        {
            var roleLabel = msg.Role switch
            {
                "user" => "[bold yellow]Você[/]",
                "assistant" => "[bold green]AI Kernel[/]",
                "system" => "[bold blue]Sistema[/]",
                "error" => "[bold red]Erro[/]",
                _ => $"[bold]{msg.Role}[/]"
            };

            var content = msg.Content.Length > 200
                ? msg.Content[..200] + "..."
                : msg.Content;

            if (msg.IsError)
                lines.Add($"[red]{roleLabel}:[/] {content.EscapeMarkup()}");
            else
                lines.Add($"{roleLabel}: {content.EscapeMarkup()}");

            lines.Add("");
        }

        if (_messages.Count == 0)
            lines.Add("[grey]Nenhuma mensagem ainda. Digite /help para comandos.[/]");

        return new Panel(
                new Rows(
                    lines.TakeLast(30).Select(l => new Markup(l)).ToArray()
                )
            )
            .Header(" Chat ")
            .Border(BoxBorder.Rounded)
            .Expand();
    }

    public int MessageCount => _messages.Count;

    public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();

    public void Clear()
    {
        _messages.Clear();
    }
}

public sealed record ChatMessage(string Role, string Content, bool IsError, DateTimeOffset Timestamp);
