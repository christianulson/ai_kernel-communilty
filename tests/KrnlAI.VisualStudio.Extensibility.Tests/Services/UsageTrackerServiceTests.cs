using FluentAssertions;
using KrnlAI.VisualStudio.Extensibility.Core.Services;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests.Services;

public sealed class UsageTrackerServiceTests
{
    [Fact]
    public void TrackCommand_ShouldIncrementInvocations()
    {
        var tracker = new UsageTrackerService(filePath: null);

        tracker.TrackCommand("status");

        tracker.Stats.CommandInvocations.Should().Be(1);
    }

    [Fact]
    public void TrackAgentRun_ShouldAccumulateTokens()
    {
        var tracker = new UsageTrackerService(filePath: null);

        tracker.TrackAgentRun(10, 20);
        tracker.TrackAgentRun(5, 5);

        tracker.Stats.AgentRuns.Should().Be(2);
        tracker.Stats.TokensIn.Should().Be(15);
        tracker.Stats.TokensOut.Should().Be(25);
    }

    [Fact]
    public async Task ResetAsync_ShouldZeroStats()
    {
        var tracker = new UsageTrackerService(filePath: null);
        tracker.TrackCommand("x");

        await tracker.ResetAsync();

        tracker.Stats.CommandInvocations.Should().Be(0);
    }
}