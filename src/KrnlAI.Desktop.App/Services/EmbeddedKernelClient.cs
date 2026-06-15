using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Embedded;
using KrnlAI.Embedded.Abstractions;

namespace KrnlAI.Desktop.App.Services;

public sealed class EmbeddedKernelClient : IKernelClient
{
    private readonly IEmbeddedKrnlAI _kernel;

    public EmbeddedKernelClient(IEmbeddedKrnlAI kernel)
    {
        _kernel = kernel;
    }

    public void SetBaseUrl(string baseUrl) { }
    public void SetAuthToken(string? token) { }
    public void SetTokens(string? token, string? refreshToken) { }

    public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new LoginResponse(true, Username: request.Email));

    public async Task<AgentRunResponse> RunAgentAsync(AgentRunRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _kernel.RunAsync(request.Prompt, cancellationToken);
        return new AgentRunResponse(
            result.Narration,
            null,
            result.Steps.Select((step, index) => new TransportStep($"Embedded:{index + 1}", step, result.Error is null, null)).ToList(),
            [result.Mode],
            result.Error);
    }

    public Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public async Task<MemorySearchResult> SearchMemoryAsync(string query, int topK = 10, CancellationToken cancellationToken = default)
    {
        var hits = await _kernel.SearchMemoryAsync(query, cancellationToken);
        return new MemorySearchResult(
            hits.Take(Math.Max(1, topK))
                .Select(hit => new MemoryHit(hit.Id, hit.Payload ?? "", "embedded", hit.Score, DateTime.UtcNow, null))
                .ToList(),
            hits.Count,
            0);
    }

    public Task<MemoryIngestResult> IngestMemoryAsync(MemoryIngestRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new MemoryIngestResult(false, null, 0, "Embedded runtime ingests memory through agent execution."));

    public Task<MemoryMetrics?> GetMemoryMetricsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<MemoryMetrics?>(new MemoryMetrics(0, 0, 0, [], null, null));

    public Task<WorkingMemorySummary?> GetWorkingMemoryAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<WorkingMemorySummary?>(new WorkingMemorySummary(0, 0, []));

    public Task<PolicyListResponse> GetPoliciesAsync(string? domain = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        => Task.FromResult(new PolicyListResponse([], 0, page, pageSize));

    public Task<PolicyDetails?> GetPolicyAsync(string policyId, CancellationToken cancellationToken = default)
        => Task.FromResult<PolicyDetails?>(null);

    public Task<PolicyInfo?> CreatePolicyAsync(CreatePolicyRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult<PolicyInfo?>(null);

    public Task<PolicyInfo?> UpdatePolicyAsync(string policyId, UpdatePolicyRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult<PolicyInfo?>(null);

    public Task<bool> DeletePolicyAsync(string policyId, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<EpisodeSearchResult> SearchEpisodesAsync(EpisodeSearchRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new EpisodeSearchResult([], 0, request.Page, request.PageSize));

    public Task<EpisodeDetails?> GetEpisodeAsync(string episodeId, CancellationToken cancellationToken = default)
        => Task.FromResult<EpisodeDetails?>(null);

    public Task<byte[]> GenerateSpeechAsync(string text, string? language = null, string? voice = null, CancellationToken cancellationToken = default)
        => Task.FromResult(Array.Empty<byte>());

    public Task<string?> TranscribeAudioAsync(byte[] audioData, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);

    public Task<FeedbackResponse> SubmitFeedbackAsync(FeedbackRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new FeedbackResponse(true, null, "Feedback accepted locally."));

    public Task<AgentMetricsSummary?> GetMetricsSummaryAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<AgentMetricsSummary?>(new AgentMetricsSummary(0, 0, 0, 0, 0, 0, 0, []));

    public Task<AgentScorecard?> GetScorecardAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<AgentScorecard?>(new AgentScorecard(1, 1, 1, 1, 1, 1));

    public Task<RuntimeSummary?> GetRuntimeSummaryAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<RuntimeSummary?>(new RuntimeSummary(false, true, "embedded", null, 0, 0, new Dictionary<string, string>
        {
            ["mode"] = "embedded",
            ["provider"] = _kernel.Provider
        }));

    public async Task<GoalListResponse> GetActiveGoalsAsync(CancellationToken cancellationToken = default)
    {
        var goals = await _kernel.GetKanbanGoalsAsync(cancellationToken);
        return new GoalListResponse(goals.Select(MapGoal).ToList(), goals.Count);
    }

    public async Task<GoalDetails?> GetGoalAsync(string goalId, CancellationToken cancellationToken = default)
    {
        var goals = await _kernel.GetKanbanGoalsAsync(cancellationToken);
        var goal = goals.FirstOrDefault(g => g.Id == goalId);
        return goal is null
            ? null
            : new GoalDetails(goal.Id, goal.Description, goal.Status, ToPriority(goal.Priority), goal.CreatedAt.UtcDateTime, null, goal.Deadline?.UtcDateTime, null, [], []);
    }

    public async Task<GoalInfo?> CreateGoalAsync(CreateGoalRequest request, CancellationToken cancellationToken = default)
    {
        var goal = new EmbeddedKanbanGoal(
            Guid.NewGuid().ToString("N"),
            request.Description,
            "active",
            request.Priority,
            DateTimeOffset.UtcNow,
            request.Deadline);
        var saved = await _kernel.UpsertKanbanGoalAsync(goal, cancellationToken);
        return saved ? MapGoal(goal) : null;
    }

    public Task<bool> UpdateGoalStatusAsync(string goalId, string action, CancellationToken cancellationToken = default)
        => _kernel.MoveKanbanCardAsync(goalId, MapGoalAction(action), cancellationToken);

    public Task<CognitiveDashboardData?> GetCognitiveDashboardAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<CognitiveDashboardData?>(new CognitiveDashboardData(1, [], [], new AutonomyStatus("embedded", DateTime.UtcNow, null)));

    public Task<MultimodalSearchResult?> SearchMultimodalAsync(string query, int topK = 10, CancellationToken cancellationToken = default)
        => Task.FromResult<MultimodalSearchResult?>(new MultimodalSearchResult(query, []));

    public Task<BenchmarkSummary?> GetBenchmarkSummaryAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<BenchmarkSummary?>(new BenchmarkSummary(0, 0, 0, 0, 0, []));

    public Task<CausalQueryResult?> GetCausalQueryAsync(string query, CancellationToken cancellationToken = default)
        => Task.FromResult<CausalQueryResult?>(new CausalQueryResult(query, [], []));

    public Task<CausalPrediction?> GetCausalPredictionAsync(string action, CancellationToken cancellationToken = default)
        => Task.FromResult<CausalPrediction?>(new CausalPrediction(action, "unknown", 0, []));

    public Task<AffectiveState?> GetAffectiveStateAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<AffectiveState?>(new AffectiveState(0, 0, 0, 0, DateTimeOffset.UtcNow));

    public Task<EmotionalState?> GetEmotionalStateAsync(string userId, CancellationToken cancellationToken = default)
        => Task.FromResult<EmotionalState?>(new EmotionalState(0, 0, 0, DateTimeOffset.UtcNow));

    public Task<CrossSummaryData?> GetCrossSummaryAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<CrossSummaryData?>(null);

    public Task<MetricsByGoalData?> GetMetricsByGoalAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<MetricsByGoalData?>(new MetricsByGoalData([], 0));

    public Task<PolicyVersionList?> GetPolicyVersionsAsync(string policyId, CancellationToken cancellationToken = default)
        => Task.FromResult<PolicyVersionList?>(new PolicyVersionList([]));

    public Task<List<PolicyRollbackEntry>> GetPolicyRollbacksAsync(string policyId, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<PolicyRollbackEntry>());

    public Task<GoalCycleList?> GetGoalCyclesAsync(string goalId, CancellationToken cancellationToken = default)
        => Task.FromResult<GoalCycleList?>(new GoalCycleList([]));

    public Task<List<McpServerInfo>> GetMcpServersAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new List<McpServerInfo>());

    public Task<bool> ToggleMcpServerAsync(string serverId, bool enabled, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<List<DocumentInfo>> GetDocumentsAsync(int limit = 50, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<DocumentInfo>());

    public Task<DocumentInfo?> GetDocumentStatusAsync(string documentId, CancellationToken cancellationToken = default)
        => Task.FromResult<DocumentInfo?>(null);

    public Task<ArchiveStats?> GetArchiveStatsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<ArchiveStats?>(new ArchiveStats(true, 0, []));

    public Task<VersionsInfo?> GetVersionsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<VersionsInfo?>(new VersionsInfo("embedded", ["embedded"], false, ""));

    public Task<ContractsResponse?> GetContractsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<ContractsResponse?>(new ContractsResponse("embedded", []));

    public Task<ModelRegistryDetail?> GetModelRegistryAsync(string modelId, CancellationToken cancellationToken = default)
        => Task.FromResult<ModelRegistryDetail?>(new ModelRegistryDetail(modelId, [], null));

    public Task<UserProfile?> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default)
        => Task.FromResult<UserProfile?>(new UserProfile(userId, null, null, "local", null, DateTime.UtcNow));

    public Task<bool> UpdateUserProfileAsync(UserProfile profile, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<ShareListResponse?> GetSharesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<ShareListResponse?>(new ShareListResponse([]));

    public Task<List<SnapshotInfo>> GetSnapshotsAsync(CancellationToken ct = default)
        => Task.FromResult(new List<SnapshotInfo>());

    public Task<List<ObjectiveInfo>> GetObjectivesAsync(CancellationToken ct = default)
        => Task.FromResult(new List<ObjectiveInfo>());

    public Task<ObjectiveDetail?> GetObjectiveDetailAsync(string id, CancellationToken ct = default)
        => Task.FromResult<ObjectiveDetail?>(new ObjectiveDetail(id, "", "embedded", 0, []));

    public Task<List<InvestigationInfo>> GetInvestigationsAsync(CancellationToken ct = default)
        => Task.FromResult(new List<InvestigationInfo>());

    private static GoalInfo MapGoal(EmbeddedKanbanGoal goal)
        => new(goal.Id, goal.Description, goal.Status, ToPriority(goal.Priority), goal.CreatedAt.UtcDateTime, null, goal.Deadline?.UtcDateTime, null, 0, 0);

    private static int ToPriority(double priority)
        => Math.Clamp((int)Math.Round(priority), 1, 5);

    private static string MapGoalAction(string action) => action switch
    {
        "pause" => "paused",
        "resume" => "active",
        "complete" => "completed",
        _ => action
    };
}
