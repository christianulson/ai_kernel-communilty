namespace KrnlAI.VisualStudio.Commands.ChatCommands;

public sealed class SlashCommand
{
    public string Name { get; }
    public string Description { get; }
    public Func<string, CancellationToken, Task<string>> Handler { get; }
    public Func<bool>? IsVisible { get; }

    public SlashCommand(
        string name,
        string description,
        Func<string, CancellationToken, Task<string>> handler,
        Func<bool>? isVisible = null)
    {
        Name = name;
        Description = description;
        Handler = handler;
        IsVisible = isVisible;
    }
}
