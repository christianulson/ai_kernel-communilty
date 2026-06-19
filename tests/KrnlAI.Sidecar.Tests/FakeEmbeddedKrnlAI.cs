using KrnlAI.Core.Abstractions;
using KrnlAI.Embedded;
using KrnlAI.Embedded.Abstractions;

namespace KrnlAI.Sidecar.Tests;

public sealed class FakeEmbeddedKrnlAI : IEmbeddedKrnlAI
{
    public EmbeddedKernelOptions Options { get; } = new();
    public string Provider => "fake";
    public ICognitiveStreamer? CognitiveStreamer => null;
    public ICognitiveSnapshotService? Snapshots => null;

    public Task<EmbeddedAgentRunResult> RunAsync(string input, CancellationToken ct = default)
    {
        var result = new EmbeddedAgentRunResult(
            Narration: $"Processed: {input}",
            Steps: ["input received", "fake processed"],
            Error: null,
            Mode: "test");
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<VectorHit>> SearchMemoryAsync(string query, CancellationToken ct = default)
    {
        var hits = new List<VectorHit>
        {
            new() { Id = "hit-1", Score = 0.95f, Payload = "fake result" }
        };
        return Task.FromResult<IReadOnlyList<VectorHit>>(hits);
    }

    public Task<IReadOnlyList<EmbeddedKanbanGoal>> GetKanbanGoalsAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<EmbeddedKanbanGoal>>([]);
    }

    public Task<bool> UpsertKanbanGoalAsync(EmbeddedKanbanGoal goal, CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> MoveKanbanCardAsync(string cardId, string newStatus, CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
