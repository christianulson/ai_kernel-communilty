namespace KrnlAI.Desktop.Core.Models;

public sealed record TerminalOutput(string Type, string Text);

public sealed record TerminalCommand(string Command, DateTime ExecutedAt);
