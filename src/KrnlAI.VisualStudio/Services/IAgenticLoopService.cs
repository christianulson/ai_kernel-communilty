using KrnlAI.Sdk.Models;

namespace KrnlAI.VisualStudio.Services;

public sealed record AgenticLoopResult(
    string Status,
    string? Summary,
    string? Error,
    IReadOnlyList<AgentStep>? Steps
);

public sealed record AgentStep(
    int Number,
    string Description,
    string? Result,
    bool IsCompleted
);

public interface IAgenticLoopService
{
    Task<AgenticLoopResult> ExecuteAsync(string goal, CancellationToken ct);
    Task CancelAsync();
}
