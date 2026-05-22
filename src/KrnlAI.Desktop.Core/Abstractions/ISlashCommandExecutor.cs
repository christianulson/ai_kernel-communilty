namespace KrnlAI.Desktop.Core.Abstractions;

public interface ISlashCommandExecutor
{
    Task<string> ExecuteAsync(string input, CancellationToken ct = default);
}
