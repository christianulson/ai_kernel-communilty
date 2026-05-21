using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

public sealed class UsageTrackerServiceTests
{
    [Fact]
    public void InitialStats_ShouldBeZero()
    {
        var tracker = new UsageTrackerService();

        tracker.Stats.CommandInvocations.Should().Be(0);
        tracker.Stats.AgentRuns.Should().Be(0);
        tracker.Stats.TokensIn.Should().Be(0);
        tracker.Stats.TokensOut.Should().Be(0);
        tracker.Stats.Errors.Should().Be(0);
        tracker.Stats.ApiCalls.Should().Be(0);
    }

    [Fact]
    public void TrackCommand_ShouldIncrement()
    {
        var tracker = new UsageTrackerService();

        tracker.TrackCommand("/explain");

        tracker.Stats.CommandInvocations.Should().Be(1);
    }

    [Fact]
    public void TrackAgentRun_ShouldIncrementTokens()
    {
        var tracker = new UsageTrackerService();

        tracker.TrackAgentRun(100, 50);

        tracker.Stats.AgentRuns.Should().Be(1);
        tracker.Stats.TokensIn.Should().Be(100);
        tracker.Stats.TokensOut.Should().Be(50);
    }

    [Fact]
    public void TrackError_ShouldIncrement()
    {
        var tracker = new UsageTrackerService();

        tracker.TrackError("timeout");

        tracker.Stats.Errors.Should().Be(1);
    }

    [Fact]
    public void TrackApiCall_ShouldIncrement()
    {
        var tracker = new UsageTrackerService();

        tracker.TrackApiCall();

        tracker.Stats.ApiCalls.Should().Be(1);
    }

    [Fact]
    public async Task ResetAsync_ShouldClearAllStats()
    {
        var tracker = new UsageTrackerService();
        tracker.TrackCommand("/test");
        tracker.TrackAgentRun(10, 5);
        tracker.TrackError("err");

        await tracker.ResetAsync();

        tracker.Stats.CommandInvocations.Should().Be(0);
        tracker.Stats.AgentRuns.Should().Be(0);
        tracker.Stats.TokensIn.Should().Be(0);
        tracker.Stats.TokensOut.Should().Be(0);
        tracker.Stats.Errors.Should().Be(0);
    }

    [Fact]
    public void StatsChanged_ShouldFireOnTrack()
    {
        var tracker = new UsageTrackerService();
        UsageStats? captured = null;
        tracker.StatsChanged += s => captured = s;

        tracker.TrackCommand("/test");

        captured.Should().NotBeNull();
        captured!.CommandInvocations.Should().Be(1);
    }
}
