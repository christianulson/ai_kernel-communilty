using System.Text.Json;
using Cts = KrnlAI.Contracts.Contracts;
using KrnlAI.Desktop.Core.Models;
using CoreModels = KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Tests.Models;

public sealed class ModelJsonRoundTripTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static string RoundTrip<T>(T value) =>
        JsonSerializer.Serialize(JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, JsonOpts), JsonOpts), JsonOpts);

    private static string Canonical<T>(T value) => JsonSerializer.Serialize(value, JsonOpts);

    [Fact]
    public void ChatMessage_RoundTrip()
    {
        var original = new CoreModels.ChatMessage("m1", "hello", MessageRole.User, new DateTime(2026, 5, 20, 10, 0, 0, DateTimeKind.Utc), MessageStatus.Completed);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void ChatSession_RoundTrip()
    {
        var msgs = new List<CoreModels.ChatMessage> { new("m1", "hi", MessageRole.User, DateTime.UtcNow) };
        var original = new CoreModels.ChatSession("s1", msgs, DateTime.UtcNow, DateTime.UtcNow);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void AgentRunRequest_RoundTrip()
    {
        var original = new Cts.AgentRunTransportRequest("test prompt", "gateway", "agent1", new() { { "key", "val" } });
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void LoginRequest_RoundTrip()
    {
        var original = new CoreModels.LoginRequest("user@test.com", "pass");
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void MemorySearchResult_RoundTrip()
    {
        var hits = new List<CoreModels.MemoryHit>
        {
            new("h1", "content", "web", 0.95, DateTime.UtcNow, new() { { "source", "test" } })
        };
        var original = new CoreModels.MemorySearchResult(hits, 1, 0.5);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void MemoryIngestRequest_RoundTrip()
    {
        var original = new CoreModels.MemoryIngestRequest("test content", "multimodal", "doc1");
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void PolicyInfo_RoundTrip()
    {
        var original = new CoreModels.PolicyInfo("p1", "Policy 1", "http", "1.0", DateTime.UtcNow, DateTime.UtcNow, true);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void PolicyDetails_RoundTrip()
    {
        var versions = new List<CoreModels.PolicyVersion>
        {
            new("1.0", DateTime.UtcNow, "user", "initial")
        };
        var original = new CoreModels.PolicyDetails("p1", "Policy 1", "http", "1.0", "content", DateTime.UtcNow, DateTime.UtcNow, true, versions);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void EpisodeInfo_RoundTrip()
    {
        var original = new CoreModels.EpisodeInfo("e1", "g1", "completed", DateTime.UtcNow, DateTime.UtcNow, 5000, "success", 0.95);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void EpisodeDetails_RoundTrip()
    {
        var steps = new List<CoreModels.EpisodeStep> { new(1, "analyze", "done", DateTime.UtcNow, DateTime.UtcNow, 1000, true, null) };
        var original = new CoreModels.EpisodeDetails("e1", "g1", "completed", DateTime.UtcNow, DateTime.UtcNow, 5000, "success", 0.95, "summary", steps);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void GoalInfo_RoundTrip()
    {
        var original = new CoreModels.GoalInfo("g1", "test goal", "active", 3, DateTime.UtcNow, null, null, null, 0, 0);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void GoalDetails_RoundTrip()
    {
        var sub = new List<CoreModels.SubGoal> { new("sg1", "sub task", true) };
        var cycles = new List<CoreModels.GoalCycle> { new("run", "ok", 100, DateTime.UtcNow, "g1") };
        var original = new CoreModels.GoalDetails("g1", "test", "active", 3, DateTime.UtcNow, null, null, 0.8, sub, cycles);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void AgentScorecard_RoundTrip()
    {
        var original = new CoreModels.AgentScorecard(0.95, 0.88, 0.99, 0.92, 0.85, 0.92);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void RuntimeSummary_RoundTrip()
    {
        var original = new CoreModels.RuntimeSummary(true, true, "1.0", "2.0", 5, 1024, new() { { "service1", "ok" } });
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void AffectiveState_RoundTrip()
    {
        var original = new CoreModels.AffectiveState(0.5, 0.3, 0.1, 0.8, DateTime.UtcNow);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void BenchmarkSummary_RoundTrip()
    {
        var suites = new List<CoreModels.BenchmarkSuite> { new("suite1", 10, 0.95, 100.5, 0.92) };
        var original = new CoreModels.BenchmarkSummary(1, 10, 0.95, 100.5, 0.92, suites);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void CausalQueryResult_RoundTrip()
    {
        var nodes = new List<CoreModels.CausalNode> { new("n1", "Node 1", "event", new() { { "weight", 0.5 } }) };
        var edges = new List<CoreModels.CausalEdge> { new("n1", "n2", "causes", 0.8) };
        var original = new CoreModels.CausalQueryResult("query", nodes, edges);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void CausalPrediction_RoundTrip()
    {
        var original = new CoreModels.CausalPrediction("action", "outcome", 0.85, ["factor1", "factor2"]);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void UserProfile_RoundTrip()
    {
        var original = new CoreModels.UserProfile("u1", "Test", "test@test.com", "admin", new() { { "theme", "dark" } }, DateTime.UtcNow);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void CognitiveDashboardData_RoundTrip()
    {
        var modules = new List<CoreModels.CognitiveModule> { new("module1", 0.9, "active") };
        var events = new List<CoreModels.CognitiveEvent> { new("info", "test event", "source1", DateTime.UtcNow) };
        var autonomy = new CoreModels.AutonomyStatus("high", DateTime.UtcNow, new() { { "domain1", 0.8 } });
        var original = new CoreModels.CognitiveDashboardData(0.85, modules, events, autonomy);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void PolicyVersionList_RoundTrip()
    {
        var versions = new List<CoreModels.PolicyVersionExtended>
        {
            new("p1", "1.0", DateTime.UtcNow, "user", "initial", 0.95)
        };
        var original = new CoreModels.PolicyVersionList(versions);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void ModelRegistryDetail_RoundTrip()
    {
        var models = new List<CoreModels.ModelRegistryEntry>
        {
            new("m1", "1.0", "chat", "local", "active", "admin", DateTime.UtcNow, DateTime.UtcNow)
        };
        var active = new CoreModels.ModelRegistryEntry("m1", "1.0", "chat", "local", "active", "admin", DateTime.UtcNow, DateTime.UtcNow);
        var original = new CoreModels.ModelRegistryDetail("m1", models, active);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void McpServerInfo_RoundTrip()
    {
        var original = new CoreModels.McpServerInfo("s1", "Server 1", "stdio", true, true, 5, DateTime.UtcNow);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void SessionShare_RoundTrip()
    {
        var original = new CoreModels.SessionShare("sc1", "sid1", "read", DateTime.UtcNow, DateTime.UtcNow, 5, false);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void FeedbackRequest_RoundTrip()
    {
        var original = new CoreModels.FeedbackRequest("ep1", 5, "great!", null);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void EmotionalState_RoundTrip()
    {
        var original = new CoreModels.EmotionalState(0.5, 0.3, 0.7, DateTimeOffset.UtcNow);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void CrossSummaryData_RoundTrip()
    {
        var gw = new CoreModels.CrossServiceStatus("1.0", TimeSpan.FromHours(1), 10, 100, 0, 0);
        var kernel = new CoreModels.CrossServiceStatus("2.0", TimeSpan.FromHours(2), 0, 0, 5, 10);
        var weights = new CoreModels.HybridWeightsData(0.7, 0.3, 0.5, 0.9);
        var original = new CoreModels.CrossSummaryData(gw, kernel, weights);
        Assert.Equal(Canonical(original), RoundTrip(original));
    }

    [Fact]
    public void RecordEquality_SameValues_ShouldBeEqual()
    {
        var now = DateTime.UtcNow;
        var a = new CoreModels.GoalInfo("g1", "test", "active", 3, now, null, null, null, 0, 0);
        var b = new CoreModels.GoalInfo("g1", "test", "active", 3, now, null, null, null, 0, 0);
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void RecordEquality_DifferentValues_ShouldNotBeEqual()
    {
        var a = new CoreModels.GoalInfo("g1", "test", "active", 3, DateTime.UtcNow, null, null, null, 0, 0);
        var b = new CoreModels.GoalInfo("g2", "other", "active", 3, DateTime.UtcNow, null, null, null, 0, 0);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void EmptyCollection_ShouldSerialize()
    {
        var original = new CoreModels.PolicyListResponse([], 0, 1, 20);
        var json = JsonSerializer.Serialize(original, JsonOpts);
        var restored = JsonSerializer.Deserialize<CoreModels.PolicyListResponse>(json, JsonOpts);
        Assert.NotNull(restored);
        Assert.Empty(restored!.Policies);
    }

    [Fact]
    public void NullableFields_ShouldHandleNull()
    {
        var original = new CoreModels.GoalInfo("g1", "test", "active", 3, DateTime.UtcNow, null, null, null, 0, 0);
        var json = JsonSerializer.Serialize(original, JsonOpts);
        var restored = JsonSerializer.Deserialize<CoreModels.GoalInfo>(json, JsonOpts);
        Assert.NotNull(restored);
        Assert.Null(restored!.CompletedAt);
        Assert.Null(restored.Deadline);
        Assert.Null(restored.SuccessRate);
    }
}
