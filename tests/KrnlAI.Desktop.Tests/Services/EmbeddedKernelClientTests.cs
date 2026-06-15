using KrnlAI.Core.Abstractions;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Embedded;
using KrnlAI.Embedded.Abstractions;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class EmbeddedKernelClientTests
{
    [Fact]
    public async Task EmbeddedKernelClient_RunAgentAsync_ShouldReturnEmbeddedNarration()
    {
        var client = new EmbeddedKernelClient(new FakeEmbeddedKrnlAI());

        var result = await client.RunAgentAsync(new AgentRunRequest("hello"));

        Assert.Equal("embedded: hello", result.Narration);
        Assert.Contains("embedded", result.ActiveStages!);
    }

    [Fact]
    public async Task EmbeddedKernelClient_SearchMemoryAsync_ShouldMapVectorHits()
    {
        var client = new EmbeddedKernelClient(new FakeEmbeddedKrnlAI());

        var result = await client.SearchMemoryAsync("memory", 10);

        Assert.Single(result.Hits);
        Assert.Equal("hit-1", result.Hits[0].Id);
        Assert.Equal("payload", result.Hits[0].Content);
    }

    [Fact]
    public async Task EmbeddedKernelClient_GetActiveGoalsAsync_ShouldMapKanbanGoals()
    {
        var client = new EmbeddedKernelClient(new FakeEmbeddedKrnlAI());

        var result = await client.GetActiveGoalsAsync();

        Assert.Single(result.Goals);
        Assert.Equal("goal-1", result.Goals[0].GoalId);
        Assert.Equal("active", result.Goals[0].Status);
    }

    private sealed class FakeEmbeddedKrnlAI : IEmbeddedKrnlAI
    {
        public EmbeddedKernelOptions Options { get; } = new();
        public string Provider => "fake";
        public ICognitiveStreamer? CognitiveStreamer => null;
        public ICognitiveSnapshotService? Snapshots => null;

        public Task<EmbeddedAgentRunResult> RunAsync(string input, CancellationToken ct = default)
        {
            return Task.FromResult(new EmbeddedAgentRunResult(
                $"embedded: {input}",
                ["input received"],
                null,
                "embedded"));
        }

        public Task<IReadOnlyList<VectorHit>> SearchMemoryAsync(string query, CancellationToken ct = default)
        {
            IReadOnlyList<VectorHit> hits =
            [
                new VectorHit { Id = "hit-1", Payload = "payload", Score = 0.9f }
            ];
            return Task.FromResult(hits);
        }

        public Task<IReadOnlyList<EmbeddedKanbanGoal>> GetKanbanGoalsAsync(CancellationToken ct = default)
        {
            IReadOnlyList<EmbeddedKanbanGoal> goals =
            [
                new EmbeddedKanbanGoal("goal-1", "ship runtime", "active", 4, DateTimeOffset.UtcNow, null)
            ];
            return Task.FromResult(goals);
        }

        public Task<bool> UpsertKanbanGoalAsync(EmbeddedKanbanGoal goal, CancellationToken ct = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> MoveKanbanCardAsync(string cardId, string newStatus, CancellationToken ct = default)
        {
            return Task.FromResult(cardId == "goal-1" && newStatus == "completed");
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
