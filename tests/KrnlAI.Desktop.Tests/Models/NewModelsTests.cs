namespace KrnlAI.Desktop.Tests.Models;

public class BenchmarkSummaryTests
{
    [Fact]
    public void BenchmarkSummary_ShouldCreate()
    {
        var suites = new List<BenchmarkSuite> { new("Suite1", 10, 0.95, 150, 0.98) };
        var summary = new BenchmarkSummary(2, 20, 0.85, 200.0, 0.92, suites);

        Assert.Equal(2, summary.TotalSuites);
        Assert.Equal(20, summary.TotalScenarios);
        Assert.Equal(0.85, summary.OverallScore);
        Assert.Equal(200.0, summary.AvgLatencyMs);
        Assert.Equal(0.92, summary.AvgSuccessRate);
        Assert.Single(summary.Suites);
    }

    [Fact]
    public void BenchmarkSuite_ShouldCreate()
    {
        var suite = new BenchmarkSuite("Test Suite", 15, 0.75, 300, 0.88);
        Assert.Equal("Test Suite", suite.Name);
        Assert.Equal(15, suite.Scenarios);
        Assert.Equal(0.75, suite.Score);
        Assert.Equal(300, suite.LatencyMs);
        Assert.Equal(0.88, suite.SuccessRate);
    }
}

public class CausalModelsTests
{
    [Fact]
    public void CausalNode_ShouldCreate()
    {
        var attrs = new Dictionary<string, double> { { "confidence", 0.9 } };
        var node = new CausalNode("n1", "Test Node", "event", attrs);

        Assert.Equal("n1", node.Id);
        Assert.Equal("Test Node", node.Label);
        Assert.Equal("event", node.Type);
        Assert.NotNull(node.Attributes);
        Assert.Equal(0.9, node.Attributes["confidence"]);
    }

    [Fact]
    public void CausalEdge_ShouldCreate()
    {
        var edge = new CausalEdge("n1", "n2", "causes", 0.8);
        Assert.Equal("n1", edge.SourceId);
        Assert.Equal("n2", edge.TargetId);
        Assert.Equal("causes", edge.Label);
        Assert.Equal(0.8, edge.Weight);
    }

    [Fact]
    public void CausalPrediction_ShouldCreate()
    {
        var factors = new List<string> { "factor1", "factor2" };
        var pred = new CausalPrediction("action1", "expected outcome", 0.75, factors);

        Assert.Equal("action1", pred.Action);
        Assert.Equal("expected outcome", pred.Outcome);
        Assert.Equal(0.75, pred.Probability);
        Assert.Equal(2, pred.ContributingFactors?.Count);
    }

    [Fact]
    public void CausalQueryResult_ShouldCreate()
    {
        var nodes = new List<CausalNode> { new("n1", "Node1", "event", null) };
        var edges = new List<CausalEdge> { new("n1", "n2", "causes", 0.5) };
        var result = new CausalQueryResult("test query", nodes, edges);

        Assert.Equal("test query", result.Query);
        Assert.Single(result.Nodes);
        Assert.Single(result.Edges);
    }
}

public class UserProfileTests
{
    [Fact]
    public void UserProfile_ShouldCreate()
    {
        var prefs = new Dictionary<string, string> { { "theme", "dark" } };
        var profile = new UserProfile("user1", "Test User", "test@test.com", "admin", prefs, DateTime.UtcNow);

        Assert.Equal("user1", profile.UserId);
        Assert.Equal("Test User", profile.Name);
        Assert.Equal("test@test.com", profile.Email);
        Assert.Equal("admin", profile.Role);
        Assert.NotNull(profile.Preferences);
    }
}

public class PolicyVersionModelTests
{
    [Fact]
    public void PolicyVersionExtended_ShouldCreate()
    {
        var ver = new PolicyVersionExtended("p1", "1.0", DateTime.UtcNow, "admin", "Initial version", 0.95);
        Assert.Equal("p1", ver.PolicyId);
        Assert.Equal("1.0", ver.Version);
        Assert.Equal("admin", ver.CreatedBy);
        Assert.Equal(0.95, ver.SuccessRate);
    }

    [Fact]
    public void PolicyRollbackEntry_ShouldCreate()
    {
        var entry = new PolicyRollbackEntry("rb1", "p1", "1.0", "admin", "Performance regression", DateTime.UtcNow);
        Assert.Equal("rb1", entry.RollbackId);
        Assert.Equal("p1", entry.PolicyId);
        Assert.Equal("1.0", entry.TargetVersion);
        Assert.Equal("admin", entry.PerformedBy);
    }
}
