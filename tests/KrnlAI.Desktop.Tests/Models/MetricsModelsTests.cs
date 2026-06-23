namespace KrnlAI.Desktop.Tests.Models;

public class AgentMetricsSummaryTests
{
    [Fact]
    public void AgentMetricsSummary_ShouldStoreValues()
    {
        var byGoal = new Dictionary<string, Core.Models.GoalMetrics>
        {
            { "g1", new Core.Models.GoalMetrics("g1", 10, 8, 2, 0.8, 150, 0.05) }
        };
        var summary = new Core.Models.AgentMetricsSummary(100, 80, 15, 5, 0.8, 200, 0.1, byGoal);

        Assert.Equal(100, summary.TotalRuns);
        Assert.Equal(80, summary.CompletedRuns);
        Assert.Equal(15, summary.FailedRuns);
        Assert.Equal(5, summary.AbortedRuns);
        Assert.Equal(0.8, summary.SuccessRate);
        Assert.Single(summary.ByGoal);
    }

    [Fact]
    public void AgentMetricsSummary_ShouldAllowEmptyByGoal()
    {
        var summary = new Core.Models.AgentMetricsSummary(0, 0, 0, 0, 0, 0, 0, []);
        Assert.Empty(summary.ByGoal);
    }
}

public class GoalMetricsTests
{
    [Fact]
    public void GoalMetrics_ShouldCreateWithValues()
    {
        var metrics = new Core.Models.GoalMetrics("g1", 10, 7, 3, 0.7, 200, 0.15);
        Assert.Equal("g1", metrics.GoalId);
        Assert.Equal(7, metrics.CompletedRuns);
        Assert.Equal(0.7, metrics.SuccessRate);
    }
}

public class AgentScorecardTests
{
    [Fact]
    public void AgentScorecard_ShouldStoreAllDimensions()
    {
        var sc = new Core.Models.AgentScorecard(0.95, 0.88, 0.99, 0.92, 0.85, 0.92);

        Assert.Equal(0.95, sc.Reliability);
        Assert.Equal(0.88, sc.Efficiency);
        Assert.Equal(0.99, sc.Safety);
        Assert.Equal(0.92, sc.AntiLoop);
        Assert.Equal(0.85, sc.Governance);
        Assert.Equal(0.92, sc.Overall);
    }

    [Fact]
    public void AgentScorecard_ShouldAllowPerfectScore()
    {
        var sc = new Core.Models.AgentScorecard(1.0, 1.0, 1.0, 1.0, 1.0, 1.0);
        Assert.Equal(1.0, sc.Overall);
    }
}

public class RuntimeSummaryTests
{
    [Fact]
    public void RuntimeSummary_ShouldStoreValues()
    {
        var services = new Dictionary<string, string> { { "kernel", "healthy" }, { "gateway", "healthy" } };
        var runtime = new Core.Models.RuntimeSummary(true, true, "1.0.0", "2.0.0", 5, 1024000, services);

        Assert.True(runtime.GatewayHealthy);
        Assert.True(runtime.KernelHealthy);
        Assert.Equal(5, runtime.ActiveGoals);
        Assert.Equal(2, runtime.Services.Count);
    }

    [Fact]
    public void RuntimeSummary_ShouldSupportDegradedState()
    {
        var runtime = new Core.Models.RuntimeSummary(false, true, null, null, 0, 0, []);
        Assert.False(runtime.GatewayHealthy);
        Assert.True(runtime.KernelHealthy);
        Assert.Null(runtime.KernelVersion);
    }
}

public class ObservabilitySummaryTests
{
    [Fact]
    public void ObservabilitySummary_ShouldNestAll()
    {
        var sc = new Core.Models.AgentScorecard(0.9, 0.8, 0.95, 0.9, 0.85, 0.88);
        var metrics = new Core.Models.AgentMetricsSummary(50, 40, 8, 2, 0.8, 150, 0.1, []);
        var runtime = new Core.Models.RuntimeSummary(true, true, "1.0", "2.0", 3, 512, []);
        var summary = new Core.Models.ObservabilitySummary(runtime, sc, metrics);

        Assert.Equal(0.88, summary.Scorecard.Overall);
        Assert.Equal(50, summary.Metrics.TotalRuns);
        Assert.True(summary.Runtime.KernelHealthy);
    }
}
