using AutoFixture;
using KrnlAI.Core.Abstractions;
using Cts = KrnlAI.Contracts.Contracts;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Embedded;
using KrnlAI.Embedded.Abstractions;
using Moq;
using TestHelpers;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class EmbeddedKernelClientTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();

    [Fact]
    public async Task EmbeddedKernelClient_RunAgentAsync_ShouldReturnEmbeddedNarration()
    {
        var kernel = new Mock<IEmbeddedKrnlAI>();
        kernel.Setup(x => x.RunAsync("hello", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmbeddedAgentRunResult("embedded: hello", ["input received"], null, "embedded"));

        var client = new EmbeddedKernelClient(kernel.Object);

        var result = await client.RunAgentAsync(new Cts.AgentRunTransportRequest("hello"));

        Assert.Equal("embedded: hello", result.Narration);
        Assert.Contains("embedded", result.ActiveStages!);
    }

    [Fact]
    public async Task EmbeddedKernelClient_SearchMemoryAsync_ShouldMapVectorHits()
    {
        var kernel = new Mock<IEmbeddedKrnlAI>();
        kernel.Setup(x => x.SearchMemoryAsync("memory", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<VectorHit>)new List<VectorHit>
            {
                new() { Id = "hit-1", Payload = "payload", Score = 0.9f }
            });

        var client = new EmbeddedKernelClient(kernel.Object);

        var result = await client.SearchMemoryAsync("memory", 10);

        Assert.Single(result.Hits);
        Assert.Equal("hit-1", result.Hits[0].Id);
        Assert.Equal("payload", result.Hits[0].Content);
    }

    [Fact]
    public async Task EmbeddedKernelClient_GetActiveGoalsAsync_ShouldMapKanbanGoals()
    {
        var kernel = new Mock<IEmbeddedKrnlAI>();
        kernel.Setup(x => x.GetKanbanGoalsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EmbeddedKanbanGoal>)new List<EmbeddedKanbanGoal>
            {
                new("goal-1", "ship runtime", "active", 4, DateTimeOffset.UtcNow, null)
            });

        var client = new EmbeddedKernelClient(kernel.Object);

        var result = await client.GetActiveGoalsAsync();

        Assert.Single(result.Goals);
        Assert.Equal("goal-1", result.Goals[0].GoalId);
        Assert.Equal("active", result.Goals[0].Status);
    }
}
