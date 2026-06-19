namespace KrnlAI.VisualStudio.Commands.ChatCommands;

public sealed class SlashCommand(
    string name,
    string description,
    Func<string, CancellationToken, Task<string>> handler,
    Func<bool>? isVisible = null)
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public Func<string, CancellationToken, Task<string>> Handler { get; } = handler;
    public Func<bool>? IsVisible { get; } = isVisible;
}
