using KrnlAI.Core.Abstractions;
using Cts = KrnlAI.Contracts.Contracts;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Embedded;
using KrnlAI.Embedded.Abstractions;
using Moq;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class EmbeddedKernelClientTests
{
    private static EmbeddedKernelClient CreateSut() => new(Mock.Of<IEmbeddedKrnlAI>());

    private static Mock<IEmbeddedKrnlAI> CreateKernelMock()
    {
        var kernel = new Mock<IEmbeddedKrnlAI>(MockBehavior.Strict);
        kernel.Setup(x => x.Provider).Returns("test");
        kernel.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string input, CancellationToken _) =>
                new EmbeddedAgentRunResult($"embedded: {input}", ["input received"], null, "embedded"));
        kernel.Setup(x => x.SearchMemoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<VectorHit>)[]);
        kernel.Setup(x => x.GetKanbanGoalsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EmbeddedKanbanGoal>)[]);
        kernel.Setup(x => x.UpsertKanbanGoalAsync(It.IsAny<EmbeddedKanbanGoal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        kernel.Setup(x => x.MoveKanbanCardAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return kernel;
    }

    // ────────────────────────────── Auth ──────────────────────────────
    [Fact] public void SetBaseUrl_ShouldNotThrow() => CreateSut().SetBaseUrl("http://localhost");

    [Fact] public void SetAuthToken_ShouldNotThrow() => CreateSut().SetAuthToken("token");

    [Fact] public void SetTokens_ShouldNotThrow() => CreateSut().SetTokens("token", "refresh");

    [Fact]
    public async Task LoginAsync_ShouldReturnSuccess()
    {
        var sut = CreateSut();
        var result = await sut.LoginAsync(new LoginRequest("a@b.com", "pwd"));
        Assert.True(result.Success);
        Assert.NotNull(result.Token);
        Assert.Equal("a@b.com", result.Username);
    }

    // ────────────────────────────── Health ──────────────────────────────
    [Fact]
    public async Task CheckHealthAsync_ShouldReturnTrue()
    {
        var result = await CreateSut().CheckHealthAsync();
        Assert.True(result);
    }

    // ────────────────────────────── Agent ──────────────────────────────
    [Fact]
    public async Task RunAgentAsync_ShouldMapEmbeddedResult()
    {
        var kernel = new Mock<IEmbeddedKrnlAI>();
        kernel.Setup(x => x.RunAsync("hello", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmbeddedAgentRunResult("embedded: hello", ["step1"], null, "embedded"));
        var sut = new EmbeddedKernelClient(kernel.Object);

        var result = await sut.RunAgentAsync(new Cts.AgentRunTransportRequest("hello"));

        Assert.Equal("embedded: hello", result.Narration);
        Assert.Contains("embedded", result.ActiveStages!);
    }

    // ────────────────────────────── Memory ──────────────────────────────
    [Fact]
    public async Task SearchMemoryAsync_ShouldMapVectorHits()
    {
        var kernel = CreateKernelMock();
        kernel.Setup(x => x.SearchMemoryAsync("mem", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<VectorHit>)[new() { Id = "h1", Payload = "p1", Score = 0.9f }]);
        var sut = new EmbeddedKernelClient(kernel.Object);

        var result = await sut.SearchMemoryAsync("mem", 10);

        Assert.Single(result.Hits);
        Assert.Equal("h1", result.Hits[0].Id);
        Assert.Equal("p1", result.Hits[0].Content);
    }

    [Fact]
    public async Task IngestMemoryAsync_ShouldReturnSuccess()
    {
        var result = await CreateSut().IngestMemoryAsync(new MemoryIngestRequest("content"));
        Assert.True(result.Success);
        Assert.NotNull(result.DocumentId);
    }

    [Fact]
    public async Task GetMemoryMetricsAsync_ShouldReturnDefaults()
    {
        var result = await CreateSut().GetMemoryMetricsAsync();
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalChunks);
    }

    [Fact]
    public async Task GetWorkingMemoryAsync_ShouldReturnDefaults()
    {
        var result = await CreateSut().GetWorkingMemoryAsync();
        Assert.NotNull(result);
        Assert.Equal(0, result.ActiveSlots);
    }

    // ────────────────────────────── Policies (CRUD) ──────────────────────────────
    [Fact]
    public async Task GetPoliciesAsync_InitiallyEmpty()
    {
        var result = await CreateSut().GetPoliciesAsync();
        Assert.Empty(result.Policies);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task CreatePolicyAsync_ShouldStoreAndReturn()
    {
        var sut = CreateSut();
        var result = await sut.CreatePolicyAsync(new CreatePolicyRequest("p1", "security", "content"));
        Assert.NotNull(result);
        Assert.Equal("p1", result.Name);
        Assert.Equal("security", result.Domain);

        var all = await sut.GetPoliciesAsync();
        Assert.Single(all.Policies);
    }

    [Fact]
    public async Task GetPolicyAsync_ShouldReturnCreated()
    {
        var sut = CreateSut();
        var created = await sut.CreatePolicyAsync(new CreatePolicyRequest("p1", "sec", "content"));
        var result = await sut.GetPolicyAsync(created!.Id);
        Assert.NotNull(result);
        Assert.Equal("content", result.Content);
    }

    [Fact]
    public async Task GetPolicyAsync_NotFound_ShouldReturnNull()
    {
        var result = await CreateSut().GetPolicyAsync("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdatePolicyAsync_ShouldModifyExisting()
    {
        var sut = CreateSut();
        var created = await sut.CreatePolicyAsync(new CreatePolicyRequest("p1", "sec", "old"));
        var updated = await sut.UpdatePolicyAsync(created!.Id, new UpdatePolicyRequest("p1-new", "sec", "new-content"));
        Assert.NotNull(updated);
        Assert.Equal("p1-new", updated.Name);

        var stored = await sut.GetPolicyAsync(created.Id);
        Assert.Equal("new-content", stored!.Content);
    }

    [Fact]
    public async Task UpdatePolicyAsync_NotFound_ShouldReturnNull()
    {
        var result = await CreateSut().UpdatePolicyAsync("x", new UpdatePolicyRequest("n", "d", "c"));
        Assert.Null(result);
    }

    [Fact]
    public async Task DeletePolicyAsync_ShouldRemove()
    {
        var sut = CreateSut();
        var created = await sut.CreatePolicyAsync(new CreatePolicyRequest("p1", "sec", "c"));
        var deleted = await sut.DeletePolicyAsync(created!.Id);
        Assert.True(deleted);
        Assert.Null(await sut.GetPolicyAsync(created.Id));
    }

    [Fact]
    public async Task DeletePolicyAsync_NotFound_ShouldReturnFalse()
    {
        var result = await CreateSut().DeletePolicyAsync("x");
        Assert.False(result);
    }

    [Fact]
    public async Task GetPoliciesAsync_ShouldFilterByDomain()
    {
        var sut = CreateSut();
        await sut.CreatePolicyAsync(new CreatePolicyRequest("p1", "security", "c1"));
        await sut.CreatePolicyAsync(new CreatePolicyRequest("p2", "privacy", "c2"));

        var sec = await sut.GetPoliciesAsync("security");
        Assert.Single(sec.Policies);
    }

    // ────────────────────────────── Episodes ──────────────────────────────
    [Fact]
    public async Task SearchEpisodesAsync_ShouldReturnSeeded()
    {
        var sut = CreateSut();
        var result = await sut.SearchEpisodesAsync(new EpisodeSearchRequest());
        Assert.NotEmpty(result.Episodes);
        Assert.Contains(result.Episodes, e => e.Id == "ep-1");
    }

    [Fact]
    public async Task GetEpisodeAsync_ShouldReturnSeeded()
    {
        var result = await CreateSut().GetEpisodeAsync("ep-1");
        Assert.NotNull(result);
        Assert.Equal("init", result.GoalId);
    }

    [Fact]
    public async Task GetEpisodeAsync_NotFound_ShouldReturnNull()
    {
        var result = await CreateSut().GetEpisodeAsync("x");
        Assert.Null(result);
    }

    // ────────────────────────────── Dashboard / Metrics ──────────────────────────────
    [Fact]
    public async Task GetMetricsSummaryAsync_ShouldReturnFromState()
    {
        var result = await CreateSut().GetMetricsSummaryAsync();
        Assert.NotNull(result);
        Assert.True(result.TotalRuns >= 1); // seeded episode + policies
    }

    [Fact]
    public async Task GetScorecardAsync_ShouldReturnFixed()
    {
        var result = await CreateSut().GetScorecardAsync();
        Assert.NotNull(result);
        Assert.Equal(85, result.Reliability);
    }

    [Fact]
    public async Task GetRuntimeSummaryAsync_ShouldReturnEmbeddedInfo()
    {
        var kernel = CreateKernelMock();
        var sut = new EmbeddedKernelClient(kernel.Object);
        var result = await sut.GetRuntimeSummaryAsync();
        Assert.NotNull(result);
        Assert.True(result.KernelHealthy);
        Assert.Equal("embedded", result.KernelVersion);
    }

    [Fact]
    public async Task GetCognitiveDashboardAsync_ShouldReturnData()
    {
        var result = await CreateSut().GetCognitiveDashboardAsync();
        Assert.NotNull(result);
        Assert.Equal(82, result.OverallHealth);
        Assert.NotEmpty(result.ActiveModules);
    }

    [Fact]
    public async Task GetBenchmarkSummaryAsync_ShouldReturnData()
    {
        var result = await CreateSut().GetBenchmarkSummaryAsync();
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalSuites);
        Assert.NotEmpty(result.Suites);
    }

    // ────────────────────────────── Goals ──────────────────────────────
    [Fact]
    public async Task GetActiveGoalsAsync_ShouldMapKanbanGoals()
    {
        var kernel = new Mock<IEmbeddedKrnlAI>();
        kernel.Setup(x => x.GetKanbanGoalsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EmbeddedKanbanGoal>)
            [
                new("g1", "goal desc", "active", 4, DateTimeOffset.UtcNow, null)
            ]);
        kernel.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmbeddedAgentRunResult("", [], null, ""));
        kernel.Setup(x => x.SearchMemoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<VectorHit>)[]);
        var sut = new EmbeddedKernelClient(kernel.Object);

        var result = await sut.GetActiveGoalsAsync();

        Assert.Single(result.Goals);
        Assert.Equal("g1", result.Goals[0].GoalId);
        Assert.Equal("active", result.Goals[0].Status);
    }

    [Fact]
    public async Task GetGoalAsync_ShouldReturnGoal()
    {
        var kernel = new Mock<IEmbeddedKrnlAI>();
        kernel.Setup(x => x.GetKanbanGoalsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EmbeddedKanbanGoal>)
            [
                new("g1", "goal desc", "active", 4, DateTimeOffset.UtcNow, null)
            ]);
        kernel.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmbeddedAgentRunResult("", [], null, ""));
        kernel.Setup(x => x.SearchMemoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<VectorHit>)[]);
        var sut = new EmbeddedKernelClient(kernel.Object);

        var result = await sut.GetGoalAsync("g1");
        Assert.NotNull(result);
        Assert.Equal("g1", result.GoalId);
    }

    [Fact]
    public async Task GetGoalAsync_NotFound_ShouldReturnNull()
    {
        var kernel = new Mock<IEmbeddedKrnlAI>();
        kernel.Setup(x => x.GetKanbanGoalsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EmbeddedKanbanGoal>)[]);
        kernel.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmbeddedAgentRunResult("", [], null, ""));
        kernel.Setup(x => x.SearchMemoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<VectorHit>)[]);
        var sut = new EmbeddedKernelClient(kernel.Object);

        var result = await sut.GetGoalAsync("x");
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateGoalAsync_ShouldUpsertAndReturn()
    {
        var kernel = CreateKernelMock();
        var sut = new EmbeddedKernelClient(kernel.Object);

        var result = await sut.CreateGoalAsync(new CreateGoalRequest("new goal", 3));

        Assert.NotNull(result);
        Assert.Equal("new goal", result.Description);
    }

    [Fact]
    public async Task UpdateGoalStatusAsync_ShouldCallMoveKanban()
    {
        var kernel = CreateKernelMock();
        var sut = new EmbeddedKernelClient(kernel.Object);

        var result = await sut.UpdateGoalStatusAsync("g1", "pause");

        Assert.True(result);
    }

    // ────────────────────────────── Causal ──────────────────────────────
    [Fact]
    public async Task GetCausalQueryAsync_ShouldReturnResult()
    {
        var result = await CreateSut().GetCausalQueryAsync("test query");
        Assert.NotNull(result);
        Assert.Equal("test query", result.Query);
        Assert.Empty(result.Nodes);
    }

    [Fact]
    public async Task GetCausalPredictionAsync_ShouldReturnPrediction()
    {
        var result = await CreateSut().GetCausalPredictionAsync("action");
        Assert.NotNull(result);
        Assert.Equal("action", result.Action);
    }

    // ────────────────────────────── Feedback ──────────────────────────────
    [Fact]
    public async Task SubmitFeedbackAsync_ShouldRecord()
    {
        var sut = CreateSut();
        var result = await sut.SubmitFeedbackAsync(new FeedbackRequest("ep1", 5, "great", "general"));
        Assert.True(result.Success);
        Assert.NotNull(result.FeedbackId);
    }

    [Fact]
    public async Task GetFeedbackHistoryAsync_ShouldReturnEntries()
    {
        var result = await CreateSut().GetFeedbackHistoryAsync();
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetFeedbackAverageAsync_ShouldReturnAverage()
    {
        var result = await CreateSut().GetFeedbackAverageAsync();
        Assert.NotNull(result);
        Assert.Equal(4.5, result.AverageRating);
    }

    // ────────────────────────────── Versions ──────────────────────────────
    [Fact]
    public async Task GetVersionsAsync_ShouldReturnInfo()
    {
        var result = await CreateSut().GetVersionsAsync();
        Assert.NotNull(result);
        Assert.Contains("local", result.DefaultVersion);
    }

    // ────────────────────────────── Emotional / Affective ──────────────────────────────
    [Fact]
    public async Task GetAffectiveStateAsync_ShouldReturnState()
    {
        var result = await CreateSut().GetAffectiveStateAsync();
        Assert.NotNull(result);
        Assert.Equal(0.3, result.Valence);
    }

    [Fact]
    public async Task GetEmotionalStateAsync_ShouldReturnState()
    {
        var result = await CreateSut().GetEmotionalStateAsync("user1");
        Assert.NotNull(result);
        Assert.Equal(0.24, result.Valence, 2);
    }

    [Fact]
    public async Task EmotionalHistoryAsync_ShouldReturnEntries()
    {
        var result = await CreateSut().EmotionalHistoryAsync();
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task EmotionalEventAsync_ShouldReturnTrue()
    {
        var result = await CreateSut().EmotionalEventAsync("event1");
        Assert.True(result);
    }

    // ────────────────────────────── Cross-Service ──────────────────────────────
    [Fact]
    public async Task GetCrossSummaryAsync_ShouldReturnData()
    {
        var result = await CreateSut().GetCrossSummaryAsync();
        Assert.NotNull(result);
        Assert.NotNull(result.Gateway);
    }

    [Fact]
    public async Task GetMetricsByGoalAsync_ShouldReturnEmpty()
    {
        var result = await CreateSut().GetMetricsByGoalAsync();
        Assert.NotNull(result);
        Assert.Empty(result.Goals);
    }

    // ────────────────────────────── Admin ──────────────────────────────
    [Fact]
    public async Task GetUserProfileAsync_ShouldReturnProfile()
    {
        var result = await CreateSut().GetUserProfileAsync("u1");
        Assert.NotNull(result);
        Assert.Equal("u1", result.UserId);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_ShouldReturnTrue()
    {
        var profile = new UserProfile("u1", "name", "e@m.com", "admin", null, DateTime.UtcNow);
        var result = await CreateSut().UpdateUserProfileAsync(profile);
        Assert.True(result);
    }

    [Fact]
    public async Task GetSharesAsync_ShouldReturnNull()
    {
        var result = await CreateSut().GetSharesAsync();
        Assert.Null(result);
    }

    // ────────────────────────────── Speech ──────────────────────────────
    [Fact]
    public async Task GenerateSpeechAsync_ShouldReturnWavHeader()
    {
        var result = await CreateSut().GenerateSpeechAsync("hello");
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        Assert.Equal(0x52, result[0]); // RIFF header
    }

    [Fact]
    public async Task TranscribeAudioAsync_ShouldReturnNotAvailable()
    {
        var result = await CreateSut().TranscribeAudioAsync([0x00]);
        Assert.Contains("requires a cloud", result, StringComparison.OrdinalIgnoreCase);
    }

    // ────────────────────────────── Snapshots, Objectives, Investigations ──────────────────────────────
    [Fact]
    public async Task GetSnapshotsAsync_ShouldReturnSeeded()
    {
        var result = await CreateSut().GetSnapshotsAsync();
        Assert.NotEmpty(result);
        Assert.Contains(result, s => s.SnapshotId == "ss-1");
    }

    [Fact]
    public async Task GetObjectivesAsync_ShouldReturnSeeded()
    {
        var result = await CreateSut().GetObjectivesAsync();
        Assert.NotEmpty(result);
        Assert.Contains(result, o => o.ObjectiveId == "obj-1");
    }

    [Fact]
    public async Task GetObjectiveDetailAsync_ShouldReturnDetail()
    {
        var result = await CreateSut().GetObjectiveDetailAsync("obj-1");
        Assert.NotNull(result);
        Assert.Equal("obj-1", result.ObjectiveId);
    }

    [Fact]
    public async Task GetInvestigationsAsync_ShouldReturnSeeded()
    {
        var result = await CreateSut().GetInvestigationsAsync();
        Assert.NotEmpty(result);
        Assert.Contains(result, i => i.CaseId == "inv-1");
    }

    // ────────────────────────────── Approvals ──────────────────────────────
    [Fact]
    public async Task GetPendingApprovalsAsync_InitiallyEmpty()
    {
        var result = await CreateSut().GetPendingApprovalsAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetApprovalDetailAsync_ShouldReturnNull()
    {
        var result = await CreateSut().GetApprovalDetailAsync("x");
        Assert.Null(result);
    }

    [Fact]
    public async Task ApproveRequestAsync_ShouldReturnNull()
    {
        var result = await CreateSut().ApproveRequestAsync("x");
        Assert.Null(result);
    }

    [Fact]
    public async Task RejectRequestAsync_ShouldReturnNull()
    {
        var result = await CreateSut().RejectRequestAsync("x");
        Assert.Null(result);
    }

    // ────────────────────────────── Coding ──────────────────────────────
    [Fact]
    public async Task CodingExplainAsync_ShouldReturnNotAvailable()
    {
        var result = await CreateSut().CodingExplainAsync(new CodingRequest("code", "csharp", null, null));
        Assert.NotNull(result);
        Assert.Contains("requires a cloud", result.Explanation, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task CodingFixAsync_ShouldReturnNotAvailable()
    {
        var result = await CreateSut().CodingFixAsync(new CodingRequest("code", "csharp", null, null));
        Assert.Contains("requires a cloud", result!.Explanation, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task CodingGenerateTestsAsync_ShouldReturnNotAvailable()
    {
        var result = await CreateSut().CodingGenerateTestsAsync(new CodingRequest("code", "csharp", null, null));
        Assert.Contains("requires a cloud", result!.Explanation, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task CodingReviewAsync_ShouldReturnNotAvailable()
    {
        var result = await CreateSut().CodingReviewAsync(new CodingRequest("code", "csharp", null, null));
        Assert.Contains("requires a cloud", result!.Explanation, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task CodingApplyDiffAsync_ShouldReturnNotAvailable()
    {
        var result = await CreateSut().CodingApplyDiffAsync(new CodingRequest("code", "csharp", null, null));
        Assert.Contains("requires a cloud", result!.Explanation, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task CodingCompleteAsync_ShouldReturnNotAvailable()
    {
        var result = await CreateSut().CodingCompleteAsync(new CodingRequest("code", "csharp", null, null));
        Assert.Contains("requires a cloud", result!.Explanation, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetCodingStatusAsync_ShouldReturnStatus()
    {
        var result = await CreateSut().GetCodingStatusAsync("cycle1");
        Assert.NotNull(result);
        Assert.Equal("cycle1", result.CycleId);
    }

    // ────────────────────────────── Self-Improvement ──────────────────────────────
    [Fact]
    public async Task GetSelfImprovementStatusAsync_ShouldReturnStatus()
    {
        var result = await CreateSut().GetSelfImprovementStatusAsync();
        Assert.NotNull(result);
        Assert.False(result.IsRunning);
    }

    // ────────────────────────────── Assistant (Threads) ──────────────────────────────
    [Fact]
    public async Task CreateThreadAsync_ShouldCreateAndReturn()
    {
        var result = await CreateSut().CreateThreadAsync("test");
        Assert.NotNull(result);
        Assert.Equal("test", result.Title);
    }

    [Fact]
    public async Task GetThreadAsync_ShouldReturn()
    {
        var result = await CreateSut().GetThreadAsync("t1");
        Assert.NotNull(result);
        Assert.Equal("t1", result.ThreadId);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldReturn()
    {
        var result = await CreateSut().SendMessageAsync("t1", "hello");
        Assert.NotNull(result);
        Assert.Equal("t1", result.ThreadId);
        Assert.Equal("hello", result.Content);
    }

    [Fact]
    public async Task GetMessagesAsync_ShouldReturnEmpty()
    {
        var result = await CreateSut().GetMessagesAsync("t1");
        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateRunAsync_ShouldReturn()
    {
        var result = await CreateSut().CreateRunAsync("t1");
        Assert.NotNull(result);
        Assert.Equal("t1", result.ThreadId);
    }

    [Fact]
    public async Task GetRunAsync_ShouldReturn()
    {
        var result = await CreateSut().GetRunAsync("t1", "r1");
        Assert.NotNull(result);
        Assert.Equal("r1", result.RunId);
    }

    [Fact]
    public async Task CancelRunAsync_ShouldReturnTrue()
    {
        var result = await CreateSut().CancelRunAsync("t1", "r1");
        Assert.True(result);
    }

    // ────────────────────────────── MCP ──────────────────────────────
    [Fact]
    public async Task GetMcpServersAsync_ShouldReturnSeeded()
    {
        var result = await CreateSut().GetMcpServersAsync();
        Assert.NotEmpty(result);
        Assert.Contains(result, s => s.ServerId == "local-fs");
    }

    [Fact]
    public async Task ToggleMcpServerAsync_ShouldToggleEnabled()
    {
        var sut = CreateSut();
        var result = await sut.ToggleMcpServerAsync("local-fs", false);
        Assert.True(result);

        var servers = await sut.GetMcpServersAsync();
        var fs = servers.First(s => s.ServerId == "local-fs");
        Assert.False(fs.Enabled);
    }

    [Fact]
    public async Task ToggleMcpServerAsync_NotFound_ShouldReturnFalse()
    {
        var result = await CreateSut().ToggleMcpServerAsync("x", true);
        Assert.False(result);
    }

    [Fact]
    public async Task GetMcpServerConfigAsync_ShouldReturnConfig()
    {
        var result = await CreateSut().GetMcpServerConfigAsync("local-fs");
        Assert.NotNull(result);
        Assert.Equal("local-fs", result.ServerId);
    }

    [Fact]
    public async Task UpdateMcpServerAsync_ShouldReturnTrue()
    {
        var config = new McpServerConfig("local-fs", "fs", "stdio", "", null, null);
        var result = await CreateSut().UpdateMcpServerAsync("local-fs", config);
        Assert.True(result);
    }

    // ────────────────────────────── Documents ──────────────────────────────
    [Fact]
    public async Task GetDocumentsAsync_ShouldReturnSeeded()
    {
        var result = await CreateSut().GetDocumentsAsync();
        Assert.NotEmpty(result);
        Assert.Contains(result, d => d.DocumentId == "doc-1");
    }

    [Fact]
    public async Task GetDocumentStatusAsync_ShouldReturn()
    {
        var result = await CreateSut().GetDocumentStatusAsync("doc-1");
        Assert.NotNull(result);
        Assert.Equal("README.md", result.FileName);
    }

    [Fact]
    public async Task GetDocumentStatusAsync_NotFound_ShouldReturnNull()
    {
        var result = await CreateSut().GetDocumentStatusAsync("x");
        Assert.Null(result);
    }

    // ────────────────────────────── Archive, Contracts, ModelRegistry ──────────────────────────────
    [Fact]
    public async Task GetArchiveStatsAsync_ShouldReturnStats()
    {
        var result = await CreateSut().GetArchiveStatsAsync();
        Assert.NotNull(result);
        Assert.True(result.Ok);
    }

    [Fact]
    public async Task GetContractsAsync_ShouldReturnContracts()
    {
        var result = await CreateSut().GetContractsAsync();
        Assert.NotNull(result);
        Assert.Equal("2.1", result.DefaultApiVersion);
    }

    [Fact]
    public async Task GetModelRegistryAsync_ShouldReturnDetail()
    {
        var result = await CreateSut().GetModelRegistryAsync("m1");
        Assert.NotNull(result);
        Assert.Equal("m1", result.ModelId);
    }

    // ────────────────────────────── Policy Extensions / Goal Cycles ──────────────────────────────
    [Fact]
    public async Task GetPolicyVersionsAsync_ShouldReturnVersions()
    {
        var sut = CreateSut();
        var policy = await sut.CreatePolicyAsync(new CreatePolicyRequest("p1", "security", "content"));
        var result = await sut.GetPolicyVersionsAsync(policy!.Id);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Versions);
    }

    [Fact]
    public async Task GetPolicyRollbacksAsync_ShouldReturnEmpty()
    {
        var result = await CreateSut().GetPolicyRollbacksAsync("p1");
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetGoalCyclesAsync_ShouldReturnCycles()
    {
        var result = await CreateSut().GetGoalCyclesAsync("g1");
        Assert.NotNull(result);
        Assert.NotEmpty(result.Cycles);
    }

    // ────────────────────────────── Plugins / Safety ──────────────────────────────
    [Fact]
    public async Task GetPluginsAsync_ShouldDelegateToMcpServers()
    {
        var result = await CreateSut().GetPluginsAsync();
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetSafetyReportAsync_ShouldDelegateToBenchmark()
    {
        var result = await CreateSut().GetSafetyReportAsync();
        Assert.NotNull(result);
    }

    // ────────────────────────────── Scheduled Tasks / Memory Moments ──────────────────────────────
    [Fact]
    public async Task GetScheduledTasksAsync_ShouldReturnEmpty()
    {
        var result = await CreateSut().GetScheduledTasksAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMemoryMomentsAsync_ShouldReturnSeeded()
    {
        var result = await CreateSut().GetMemoryMomentsAsync();
        Assert.NotEmpty(result);
        Assert.Contains(result, m => m.MomentId == "mm-1");
    }

    // ────────────────────────────── Knowledge ──────────────────────────────
    [Fact]
    public async Task KnowledgeAskAsync_ShouldReturnResult()
    {
        var result = await CreateSut().KnowledgeAskAsync("test query");
        Assert.NotNull(result);
        Assert.Equal("test query", result.Query);
        Assert.NotEmpty(result.Hits);
    }

    [Fact]
    public async Task KnowledgeStatsAsync_ShouldReturnStats()
    {
        var sut = CreateSut();
        await sut.KnowledgeLearnAsync("content1", "source1");
        await sut.KnowledgeLearnAsync("content2", "source2");
        var result = await sut.KnowledgeStatsAsync();
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalEntries);
    }

    [Fact]
    public async Task KnowledgeLearnAsync_ShouldReturnSuccess()
    {
        var result = await CreateSut().KnowledgeLearnAsync("content", "source", "category");
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.EntryId);
    }

    // ────────────────────────────── PIE ──────────────────────────────
    [Fact]
    public async Task PieInferAsync_ShouldReturnInference()
    {
        var result = await CreateSut().PieInferAsync("premise", "context");
        Assert.NotNull(result);
        Assert.Contains("premise", result.Conclusion);
        Assert.Equal(0.85, result.Confidence);
    }

    [Fact]
    public async Task PieChainAsync_ShouldReturnChain()
    {
        var result = await CreateSut().PieChainAsync("premise", 3);
        Assert.NotNull(result);
        Assert.Equal(3, result.Steps.Count);
    }

    [Fact]
    public async Task PieKnowledgeAsync_ShouldReturnSuccess()
    {
        var result = await CreateSut().PieKnowledgeAsync("math", "2+2=4");
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task PieCoherenceAsync_ShouldReturnData()
    {
        var result = await CreateSut().PieCoherenceAsync();
        Assert.NotNull(result);
        Assert.Equal(0.82, result.OverallCoherence);
        Assert.NotEmpty(result.Entries);
    }

    [Fact]
    public async Task PieTermsAsync_ShouldReturnTerms()
    {
        var result = await CreateSut().PieTermsAsync();
        Assert.NotEmpty(result);
        Assert.Contains(result, t => t.Id == "t1");
    }

    // ────────────────────────────── Events ──────────────────────────────
    [Fact]
    public async Task EventsRecentAsync_ShouldReturnEvents()
    {
        var result = await CreateSut().EventsRecentAsync();
        Assert.NotEmpty(result);
        Assert.Contains(result, e => e.EventId == "e1");
    }

    [Fact]
    public async Task EventDetailAsync_ShouldReturnDetail()
    {
        var result = await CreateSut().EventDetailAsync("e1");
        Assert.NotNull(result);
        Assert.Equal("e1", result.EventId);
    }

    [Fact]
    public async Task EventsByMomentAsync_ShouldReturnEvents()
    {
        var result = await CreateSut().EventsByMomentAsync("started");
        Assert.NotEmpty(result);
    }

    // ────────────────────────────── Multimodal ──────────────────────────────
    [Fact]
    public async Task SearchMultimodalAsync_ShouldReturnResult()
    {
        var result = await CreateSut().SearchMultimodalAsync("test");
        Assert.NotNull(result);
        Assert.Equal("test", result.Query);
        Assert.Empty(result.Hits);
    }

    // ────────────────────────────── Cognitive Flow ──────────────────────────────
    [Fact]
    public async Task CognitiveFlowExecuteAsync_ShouldReturnNotAvailable()
    {
        var flow = new FlowDefinition("f1", "desc", [], []);
        var result = await CreateSut().CognitiveFlowExecuteAsync(flow);
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("requires the", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CognitiveFlowSaveAsync_ShouldReturnFalse()
    {
        var flow = new FlowDefinition("f1", "desc", [], []);
        var result = await CreateSut().CognitiveFlowSaveAsync(flow);
        Assert.False(result);
    }

    [Fact]
    public async Task CognitiveFlowLoadAsync_ShouldReturnNull()
    {
        var result = await CreateSut().CognitiveFlowLoadAsync("f1");
        Assert.Null(result);
    }

    // ────────────────────────────── User Services ──────────────────────────────
    [Fact]
    public async Task GetUserServicesAsync_ShouldReturnEmpty()
    {
        var result = await CreateSut().GetUserServicesAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateUserServiceAsync_ShouldReturnTrue()
    {
        var request = new UserServiceUpdateRequest(new Dictionary<string, string>(), true);
        var result = await CreateSut().UpdateUserServiceAsync("slack", request);
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteUserServiceAsync_ShouldReturnTrue()
    {
        var result = await CreateSut().DeleteUserServiceAsync("slack");
        Assert.True(result);
    }

    // ────────────────────────────── Plan ──────────────────────────────
    [Fact]
    public async Task GetCurrentPlanAsync_ShouldReturnPlan()
    {
        var result = await CreateSut().GetCurrentPlanAsync();
        Assert.NotNull(result);
        Assert.Equal("local-plan-1", result.PlanId);
        Assert.True(result.IsRunning);
        Assert.NotNull(result.CurrentPlan);
        Assert.NotEmpty(result.Steps);
    }

    [Fact]
    public async Task GetPlanStepsAsync_ShouldReturnSteps()
    {
        var result = await CreateSut().GetPlanStepsAsync("local-plan-1");
        Assert.NotEmpty(result);
        Assert.Contains(result, s => s.Description == "Inicialização do kernel");
    }

    // ────────────────────────────── Episodic Memory ──────────────────────────────
    [Fact]
    public async Task SearchEpisodicMemoryAsync_ShouldReturnSeededEpisodes()
    {
        var result = await CreateSut().SearchEpisodicMemoryAsync(new EpisodicMemorySearchRequest("init", TopK: 10));
        Assert.NotNull(result);
        Assert.NotEmpty(result.Hits);
    }

    [Fact]
    public async Task SearchEpisodicMemoryAsync_FilterByStatus_ShouldFilter()
    {
        var result = await CreateSut().SearchEpisodicMemoryAsync(new EpisodicMemorySearchRequest("", TopK: 10, Status: "completed"));
        Assert.NotNull(result);
        Assert.All(result.Hits, h => Assert.Equal("completed", h.Status));
    }

    // ────────────────────────────── Templates ──────────────────────────────
    [Fact]
    public async Task TemplateListAsync_InitiallyEmpty()
    {
        var result = await CreateSut().TemplateListAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task TemplateCreateAsync_ShouldStoreAndReturn()
    {
        var sut = CreateSut();
        var result = await sut.TemplateCreateAsync(new CreateTemplateRequest("t1", "desc", "content {{var}}", "general"));
        Assert.NotNull(result);
        Assert.Equal("t1", result.Name);

        var all = await sut.TemplateListAsync();
        Assert.Single(all);
    }

    [Fact]
    public async Task TemplateGetAsync_ShouldReturnCreated()
    {
        var sut = CreateSut();
        var created = await sut.TemplateCreateAsync(new CreateTemplateRequest("t1", "desc", "content", "general"));
        var result = await sut.TemplateGetAsync(created!.Id);
        Assert.NotNull(result);
        Assert.Equal("content", result.Content);
    }

    [Fact]
    public async Task TemplateGetAsync_NotFound_ShouldReturnNull()
    {
        var result = await CreateSut().TemplateGetAsync("x");
        Assert.Null(result);
    }

    [Fact]
    public async Task TemplateUpdateAsync_ShouldModify()
    {
        var sut = CreateSut();
        var created = await sut.TemplateCreateAsync(new CreateTemplateRequest("t1", "desc", "old", "general"));
        var updated = await sut.TemplateUpdateAsync(created!.Id, new UpdateTemplateRequest(Name: "t1-new", Content: "new"));
        Assert.NotNull(updated);
        Assert.Equal("t1-new", updated.Name);
    }

    [Fact]
    public async Task TemplateUpdateAsync_NotFound_ShouldReturnNull()
    {
        var result = await CreateSut().TemplateUpdateAsync("x", new UpdateTemplateRequest(Name: "n"));
        Assert.Null(result);
    }

    [Fact]
    public async Task TemplateDeleteAsync_ShouldRemove()
    {
        var sut = CreateSut();
        var created = await sut.TemplateCreateAsync(new CreateTemplateRequest("t1", "desc", "c", "general"));
        var deleted = await sut.TemplateDeleteAsync(created!.Id);
        Assert.True(deleted);
        Assert.Null(await sut.TemplateGetAsync(created.Id));
    }

    [Fact]
    public async Task TemplateDeleteAsync_NotFound_ShouldReturnFalse()
    {
        var result = await CreateSut().TemplateDeleteAsync("x");
        Assert.False(result);
    }

    [Fact]
    public async Task TemplateRenderAsync_ShouldSubstituteVariables()
    {
        var sut = CreateSut();
        var created = await sut.TemplateCreateAsync(new CreateTemplateRequest("t1", "desc", "Hello {{name}}!", "general"));
        var result = await sut.TemplateRenderAsync(created!.Id, new RenderTemplateRequest(new() { ["name"] = "World" }));
        Assert.NotNull(result);
        Assert.Equal("Hello World!", result.RenderedContent);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task TemplateRenderAsync_NotFound_ShouldReturnError()
    {
        var result = await CreateSut().TemplateRenderAsync("x", new RenderTemplateRequest(new()));
        Assert.NotNull(result);
        Assert.Null(result.RenderedContent);
        Assert.NotNull(result.Error);
    }

    // ────────────────────────────── Experiments ──────────────────────────────
    [Fact]
    public async Task ExperimentListAsync_InitiallyEmpty()
    {
        var result = await CreateSut().ExperimentListAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExperimentStartAsync_ShouldCreateAndReturn()
    {
        var sut = CreateSut();
        var result = await sut.ExperimentStartAsync(new StartExperimentRequest("exp1", "desc"));
        Assert.NotNull(result);
        Assert.Equal("exp1", result.Name);
        Assert.Equal("running", result.Status);

        var all = await sut.ExperimentListAsync();
        Assert.Single(all);
    }

    [Fact]
    public async Task ExperimentCompleteAsync_ShouldComplete()
    {
        var sut = CreateSut();
        var created = await sut.ExperimentStartAsync(new StartExperimentRequest("exp1"));
        var completed = await sut.ExperimentCompleteAsync(created!.Id);
        Assert.True(completed);

        var all = await sut.ExperimentListAsync();
        var exp = all.First();
        Assert.Equal("completed", exp.Status);
    }

    [Fact]
    public async Task ExperimentCompleteAsync_NotFound_ShouldReturnFalse()
    {
        var result = await CreateSut().ExperimentCompleteAsync("x");
        Assert.False(result);
    }

    [Fact]
    public async Task ExperimentRecordMetricAsync_ShouldRecord()
    {
        var sut = CreateSut();
        var created = await sut.ExperimentStartAsync(new StartExperimentRequest("exp1"));
        var result = await sut.ExperimentRecordMetricAsync(created!.Id, new RecordMetricRequest("accuracy", 0.95));
        Assert.True(result);
    }

    [Fact]
    public async Task ExperimentRecordMetricAsync_NotFound_ShouldReturnFalse()
    {
        var result = await CreateSut().ExperimentRecordMetricAsync("x", new RecordMetricRequest("m", 1));
        Assert.False(result);
    }

    [Fact]
    public async Task ExperimentGetAnalysisAsync_ShouldReturnAnalysis()
    {
        var sut = CreateSut();
        var created = await sut.ExperimentStartAsync(new StartExperimentRequest("exp1"));
        await sut.ExperimentRecordMetricAsync(created!.Id, new RecordMetricRequest("accuracy", 0.95));
        await sut.ExperimentRecordMetricAsync(created!.Id, new RecordMetricRequest("latency", 0.85));

        var analysis = await sut.ExperimentGetAnalysisAsync(created.Id);
        Assert.NotNull(analysis);
        Assert.Equal(2, analysis.TotalMetrics);
        Assert.Equal(0.90, analysis.AvgValue, 2);
    }

    [Fact]
    public async Task ExperimentGetAnalysisAsync_NotFound_ShouldReturnNull()
    {
        var result = await CreateSut().ExperimentGetAnalysisAsync("x");
        Assert.Null(result);
    }

    // ────────────────────────────── IEmbeddedKrnlAI properties ──────────────────────────────
    [Fact]
    public async Task RuntimeSummary_ShouldIncludeProvider()
    {
        var kernel = CreateKernelMock();
        var sut = new EmbeddedKernelClient(kernel.Object);
        var result = await sut.GetRuntimeSummaryAsync();
        Assert.NotNull(result);
        Assert.Equal("test", result.Services["provider"]);
    }
}
