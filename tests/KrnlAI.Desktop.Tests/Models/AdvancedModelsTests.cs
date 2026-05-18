using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Tests.Models;

public sealed class AdvancedModelsTests
{
    [Fact]
    public void CausalNode_ShouldSetProperties()
    {
        var node = new CausalNode("n1", "Node 1", "event", new() { ["k"] = 1.0 });

        Assert.Equal("n1", node.Id);
        Assert.Equal("Node 1", node.Label);
        Assert.Equal("event", node.Type);
        Assert.Equal(1.0, node.Attributes!["k"]);
    }

    [Fact]
    public void CausalEdge_ShouldSetProperties()
    {
        var edge = new CausalEdge("s1", "t1", "causes", 0.8);

        Assert.Equal("s1", edge.SourceId);
        Assert.Equal("t1", edge.TargetId);
        Assert.Equal("causes", edge.Label);
        Assert.Equal(0.8, edge.Weight);
    }

    [Fact]
    public void CausalPrediction_ShouldSetProperties()
    {
        var pred = new CausalPrediction("run", "done", 0.9, ["factor1"]);

        Assert.Equal("run", pred.Action);
        Assert.Equal("done", pred.Outcome);
        Assert.Equal(0.9, pred.Probability);
        Assert.NotNull(pred.ContributingFactors);
        Assert.Contains("factor1", pred.ContributingFactors);
    }

    [Fact]
    public void CausalQueryResult_ShouldSetProperties()
    {
        var nodes = new List<CausalNode> { new("n1", "N1", "event", null) };
        var edges = new List<CausalEdge> { new("n1", "n2", "link", 0.5) };
        var result = new CausalQueryResult("query", nodes, edges);

        Assert.Equal("query", result.Query);
        Assert.Single(result.Nodes);
        Assert.Single(result.Edges);
    }

    [Fact]
    public void BenchmarkSummary_ShouldSetProperties()
    {
        var suite = new BenchmarkSuite("suite1", 10, 0.95, 100, 0.9);
        var summary = new BenchmarkSummary(1, 10, 0.95, 100, 0.9, [suite]);

        Assert.Equal(1, summary.TotalSuites);
        Assert.Equal(10, summary.TotalScenarios);
        Assert.Equal(0.95, summary.OverallScore);
        Assert.Single(summary.Suites);
    }

    [Fact]
    public void BenchmarkSuite_ShouldSetProperties()
    {
        var suite = new BenchmarkSuite("perf", 5, 0.8, 200, 0.85);

        Assert.Equal("perf", suite.Name);
        Assert.Equal(5, suite.Scenarios);
        Assert.Equal(0.8, suite.Score);
        Assert.Equal(200, suite.LatencyMs);
    }

    [Fact]
    public void UserProfile_ShouldSetProperties()
    {
        var profile = new UserProfile("u1", "Alice", "alice@test.com", "admin", null, null);

        Assert.Equal("u1", profile.UserId);
        Assert.Equal("Alice", profile.Name);
        Assert.Equal("admin", profile.Role);
    }

    [Fact]
    public void CognitiveDashboardData_ShouldSetProperties()
    {
        var module = new CognitiveModule("cpu", 1.0, "healthy");
        var ev = new CognitiveEvent("info", "started", "system", DateTime.UtcNow);
        var autonomy = new AutonomyStatus("full", DateTime.UtcNow, null);
        var data = new CognitiveDashboardData(0.95, [module], [ev], autonomy);

        Assert.Equal(0.95, data.OverallHealth);
        Assert.Single(data.ActiveModules);
        Assert.Single(data.RecentEvents);
        Assert.NotNull(data.Autonomy);
    }

    [Fact]
    public void CognitiveModule_ShouldSetProperties()
    {
        var module = new CognitiveModule("memory", 0.7, "degraded");

        Assert.Equal("memory", module.Name);
        Assert.Equal(0.7, module.HealthScore);
        Assert.Equal("degraded", module.Status);
    }

    [Fact]
    public void CognitiveEvent_ShouldSetProperties()
    {
        var ev = new CognitiveEvent("alert", "high cpu", "monitor", DateTime.UtcNow);

        Assert.Equal("alert", ev.Type);
        Assert.Equal("high cpu", ev.Description);
        Assert.Equal("monitor", ev.Source);
    }

    [Fact]
    public void AutonomyStatus_ShouldSetProperties()
    {
        var status = new AutonomyStatus("medium", DateTime.UtcNow, new() { ["domain1"] = 0.8 });

        Assert.Equal("medium", status.Level);
        Assert.Equal(0.8, status.DomainConfidence!["domain1"]);
    }

    [Fact]
    public void MultimodalSearchResult_ShouldSetProperties()
    {
        var hit = new MultimodalHit("h1", "content", "image", 0.95, null);
        var result = new MultimodalSearchResult("query", [hit]);

        Assert.Equal("query", result.Query);
        Assert.Single(result.Hits);
    }

    [Fact]
    public void MultimodalHit_ShouldSetProperties()
    {
        var hit = new MultimodalHit("h1", "text content", "text", 0.9, "base64data");

        Assert.Equal("h1", hit.Id);
        Assert.Equal("text content", hit.Content);
        Assert.Equal("text", hit.Modality);
        Assert.Equal(0.9, hit.Score);
        Assert.Equal("base64data", hit.ThumbnailBase64);
    }

    [Fact]
    public void CrossSummaryData_ShouldSetProperties()
    {
        var gw = new CrossServiceStatus("1.0", TimeSpan.FromHours(1), 0, 100, 5, 10);
        var kernel = new CrossServiceStatus("2.0", TimeSpan.FromHours(2), 1, 200, 3, 8);
        var weights = new HybridWeightsData(0.7, 0.2, 0.05, 0.05);
        var data = new CrossSummaryData(gw, kernel, weights);

        Assert.Equal("1.0", data.Gateway.Version);
        Assert.Equal("2.0", data.Kernel.Version);
        Assert.Equal(0.7, data.HybridWeights!.Semantic);
    }

    [Fact]
    public void CrossServiceStatus_ShouldSetProperties()
    {
        var status = new CrossServiceStatus("1.0", TimeSpan.FromMinutes(30), 2, 50, 4, 7);

        Assert.Equal("1.0", status.Version);
        Assert.Equal(TimeSpan.FromMinutes(30), status.Uptime);
        Assert.Equal(2, status.ActiveLimiters);
        Assert.Equal(50, status.RequestsPerMinute);
    }

    [Fact]
    public void HybridWeightsData_ShouldSetProperties()
    {
        var w = new HybridWeightsData(0.7, 0.2, 0.05, 0.05);

        Assert.Equal(0.7, w.Semantic);
        Assert.Equal(0.2, w.Lexical);
        Assert.Equal(0.05, w.Recency);
        Assert.Equal(0.05, w.Confidence);
    }

    [Fact]
    public void MetricsByGoalData_ShouldSetProperties()
    {
        var data = new MetricsByGoalData([], 0);

        Assert.Empty(data.Goals);
        Assert.Equal(0, data.TotalCount);
    }

    [Fact]
    public void PolicyVersionList_ShouldSetProperties()
    {
        var ver = new PolicyVersionExtended("p1", "v2", DateTime.UtcNow, "admin", "fix", 0.95);
        var list = new PolicyVersionList([ver]);

        Assert.Single(list.Versions);
        Assert.Equal("p1", list.Versions[0].PolicyId);
    }

    [Fact]
    public void PolicyRollbackEntry_ShouldSetProperties()
    {
        var entry = new PolicyRollbackEntry("r1", "p1", "v1", "admin", "bug", DateTime.UtcNow);

        Assert.Equal("r1", entry.RollbackId);
        Assert.Equal("p1", entry.PolicyId);
        Assert.Equal("admin", entry.PerformedBy);
    }

    [Fact]
    public void GoalCycleList_ShouldSetProperties()
    {
        var cycle = new GoalCycleSummary("g1", "run", "completed", DateTime.UtcNow, 100);
        var list = new GoalCycleList([cycle]);

        Assert.Single(list.Cycles);
        Assert.Equal("g1", list.Cycles[0].GoalId);
    }

    [Fact]
    public void GoalCycleSummary_ShouldSetProperties()
    {
        var cycle = new GoalCycleSummary("g1", "execute", "active", DateTime.UtcNow, 500);

        Assert.Equal("g1", cycle.GoalId);
        Assert.Equal("execute", cycle.Action);
        Assert.Equal("active", cycle.Status);
        Assert.Equal(500, cycle.DurationMs);
    }
}
