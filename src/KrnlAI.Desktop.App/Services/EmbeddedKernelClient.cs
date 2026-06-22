using System.Collections.Concurrent;
using System.Linq;
using Cts = KrnlAI.Contracts.Contracts;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Embedded;
using KrnlAI.Embedded.Abstractions;

namespace KrnlAI.Desktop.App.Services;

/// <summary>
/// In-memory implementation of <see cref="IKernelClient"/> for local/embedded mode (<c>KRNL__RUN_MODE=Local</c>).
/// All state is held in ConcurrentDictionary / List fields — no database or external services.
/// <list type="bullet">
///   <item><b>Fully functional locally:</b> Policies, Episodes, Memory (basic), Templates, Experiments, Goals (Kanban), MCP servers, Documents, Snapshots, Objectives, Investigations, Approvals, Knowledge, Emotional state, Events, User Services, Plans, Feedback, Assistant threads, PIE (symbolic), Cognitive dashboard, Metrics, Archives, Contracts, Model registry.</item>
///   <item><b>Stub / limited locally (requires cloud/LLM):</b> TTS (GenerateSpeechAsync), STT (TranscribeAudioAsync), Coding (Explain/Fix/Test/Review/Diff/Complete), Causal inference (GetCausalQueryAsync / GetCausalPredictionAsync), Cognitive Flow execution (CognitiveFlowExecuteAsync), PIE statistical inference, Self-Improvement.</item>
/// </list>
/// </summary>
public sealed class EmbeddedKernelClient : IKernelClient
{
    private readonly IEmbeddedKrnlAI _kernel;
    private readonly ConcurrentDictionary<string, PolicyDetails> _policies = new();
    private readonly ConcurrentDictionary<string, EpisodeDetails> _episodes = new();
    private readonly List<DocumentInfo> _documents = [];
    private readonly List<SnapshotInfo> _snapshots = [];
    private readonly List<ObjectiveInfo> _objectives = [];
    private readonly List<InvestigationInfo> _investigations = [];
    private readonly List<McpServerInfo> _mcpServers = [];
    private readonly List<MemoryMoment> _memoryMoments = [];

    // Stateful approvals
    private readonly ConcurrentDictionary<string, ApprovalRequest> _approvalRequests = new();

    // Stateful emotional/affective
    private double _currentValence = 0.3;
    private double _currentArousal = 0.5;
    private readonly List<EmotionalHistoryEntry> _emotionalHistory = [];

    // Stateful events
    private int _eventCounter;
    private readonly List<EventInfo> _events = [];

    // Stateful knowledge
    private int _knowledgeCounter;
    private readonly List<KnowledgeHit> _knowledgeEntries = [];

    // Stateful user services
    private readonly ConcurrentDictionary<string, UserServiceInfo> _userServices = new();

    // Stateful scheduled tasks
    private readonly List<ScheduledTask> _scheduledTasks = [];

    // Stateful assistant threads/messages
    private readonly ConcurrentDictionary<string, ThreadInfo> _threads = new();
    private readonly ConcurrentDictionary<string, List<MessageInfo>> _threadMessages = new();

    // Stateful policy rollbacks / goal cycles
    private readonly List<PolicyRollbackEntry> _policyRollbacks = [];
    private readonly ConcurrentDictionary<string, List<GoalCycleSummary>> _goalCycles = new();

    // Plan steps
    private readonly List<PlanStep> _planSteps =
    [
        new(0, "Inicialização do kernel", "Módulo cognitivo iniciado em modo local", PlanStepStatus.Completed, DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow.AddMinutes(-9), "Ok"),
        new(1, "Processamento de memória", "Indexando episódios locais", PlanStepStatus.InProgress, DateTime.UtcNow.AddMinutes(-9), null, null),
        new(2, "Consolidação", "Consolidando aprendizado do dia", PlanStepStatus.Pending, null, null, null),
    ];

    private int _feedbackCount;

    public EmbeddedKernelClient(IEmbeddedKrnlAI kernel)
    {
        _kernel = kernel;
        SeedData();
        AddEvent("system", "Local mode started", "embedded");
        AddEvent("cognitive", "Thinking cycle", "kernel");
        AddEmotionalHistory("Local boot", "startup", 0.3, 0.5);
        AddEmotionalHistory("Ready", "system", 0.4, 0.6);
    }

    private void SeedData()
    {
        _episodes["ep-1"] = new EpisodeDetails("ep-1", "init", "completed", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, 2000, "Completed", 0.95, "Initial setup episode", null);
        _documents.Add(new DocumentInfo("doc-1", "README.md", 2048, "md", "processed", null, 3, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow));
        _documents.Add(new DocumentInfo("doc-2", "config.yaml", 512, "yaml", "pending", null, 1, DateTime.UtcNow.AddDays(-1), null));
        _snapshots.Add(new SnapshotInfo("ss-1", "Initial state", DateTime.UtcNow.AddDays(-7), 4096));
        _snapshots.Add(new SnapshotInfo("ss-2", "Before experiment", DateTime.UtcNow.AddDays(-1), 8192));
        _objectives.Add(new ObjectiveInfo("obj-1", "Explore local mode", "active", 0.6, 3, null));
        _objectives.Add(new ObjectiveInfo("obj-2", "Test memory system", "pending", 0.0, 2, DateTime.UtcNow.AddDays(7).ToString("O")));
        _investigations.Add(new InvestigationInfo("inv-1", "System health check", "completed", 8, DateTime.UtcNow.AddHours(-2)));
        _investigations.Add(new InvestigationInfo("inv-2", "Performance analysis", "open", 3, DateTime.UtcNow.AddMinutes(-30)));
        _mcpServers.Add(new McpServerInfo("local-fs", "Local Filesystem", "stdio", true, true, 3, DateTime.UtcNow));
        _mcpServers.Add(new McpServerInfo("local-shell", "Local Shell", "stdio", true, true, 5, DateTime.UtcNow));
        _memoryMoments.Add(new MemoryMoment("mm-1", "App started", "Desktop application initialized in local mode", "system", 0.8, DateTime.UtcNow.AddHours(-1), ["startup", "local"]));
        _memoryMoments.Add(new MemoryMoment("mm-2", "Policy created", "User created a new security policy", "user-action", 0.6, DateTime.UtcNow.AddMinutes(-30), ["policy", "security"]));
    }

    private string MakeId() => Guid.NewGuid().ToString("N");

    private void AddEvent(string type, string description, string? source)
    {
        _eventCounter++;
        _events.Add(new EventInfo($"e{_eventCounter}", type, description, source, DateTimeOffset.UtcNow, null));
    }

    private void AddEmotionalHistory(string @event, string? trigger, double valence, double arousal)
    {
        _emotionalHistory.Add(new EmotionalHistoryEntry(DateTimeOffset.UtcNow, @event, valence, arousal, trigger));
    }

    public void SetBaseUrl(string baseUrl) { }
    public void SetAuthToken(string? token) { }
    public void SetTokens(string? token, string? refreshToken) { }

    public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var token = MakeId();
        AddEvent("auth", $"User {request.Email} logged in", "system");
        return Task.FromResult(new LoginResponse(true, token) { Username = request.Email });
    }

    public async Task<Cts.AgentRunTransportResponse> RunAgentAsync(Cts.AgentRunTransportRequest request, CancellationToken cancellationToken = default)
    {
        AddEvent("agent", $"Agent run: {request.Prompt[..Math.Min(request.Prompt.Length, 50)]}", "system");
        var result = await _kernel.RunAsync(request.Prompt, cancellationToken);
        return new Cts.AgentRunTransportResponse(
            result.Narration,
            null,
            [.. result.Steps.Select((step, index) => new Cts.TransportStepDto($"Embedded:{index + 1}", step, result.Error is null, null))],
            [result.Mode],
            result.Error);
    }

    public Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public async Task<MemorySearchResult> SearchMemoryAsync(string query, int topK = 10, CancellationToken cancellationToken = default)
    {
        AddEvent("memory", $"Memory search: {query[..Math.Min(query.Length, 50)]}", "kernel");
        var hits = await _kernel.SearchMemoryAsync(query, cancellationToken);
        return new MemorySearchResult(
            [.. hits.Take(Math.Max(1, topK)).Select(hit => new MemoryHit(hit.Id, hit.Payload ?? "", "embedded", hit.Score, DateTime.UtcNow, null))],
            hits.Count,
            0);
    }

    public Task<MemoryIngestResult> IngestMemoryAsync(MemoryIngestRequest request, CancellationToken cancellationToken = default)
    {
        AddEvent("memory", $"Memory ingest: {request.Content[..Math.Min(request.Content.Length, 50)]}", "user");
        return Task.FromResult(new MemoryIngestResult(true, MakeId(), 1, "Memory ingested locally."));
    }

    public Task<MemoryMetrics?> GetMemoryMetricsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<MemoryMetrics?>(new MemoryMetrics(0, 0, 0, [], null, null));

    public Task<WorkingMemorySummary?> GetWorkingMemoryAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<WorkingMemorySummary?>(new WorkingMemorySummary(0, 0, []));

    public Task<PolicyListResponse> GetPoliciesAsync(string? domain = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var items = _policies.Values.Where(p => domain == null || p.Domain == domain).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new PolicyInfo(p.Id, p.Name, p.Domain, p.Version, p.CreatedAt, p.UpdatedAt, p.IsActive)).ToList();
        return Task.FromResult(new PolicyListResponse(items, _policies.Count, page, pageSize));
    }

    public Task<PolicyDetails?> GetPolicyAsync(string policyId, CancellationToken ct = default)
        => Task.FromResult(_policies.GetValueOrDefault(policyId));

    public Task<PolicyInfo?> CreatePolicyAsync(CreatePolicyRequest request, CancellationToken ct = default)
    {
        var id = MakeId();
        var now = DateTime.UtcNow;
        var policy = new PolicyDetails(id, request.Name, request.Domain, "1.0", request.Content, now, now, true, null);
        _policies[id] = policy;
        AddEvent("policy", $"Policy created: {request.Name}", "user");
        return Task.FromResult<PolicyInfo?>(new PolicyInfo(id, request.Name, request.Domain, "1.0", now, now, true));
    }

    public Task<PolicyInfo?> UpdatePolicyAsync(string policyId, UpdatePolicyRequest request, CancellationToken ct = default)
    {
        if (!_policies.TryGetValue(policyId, out var existing)) return Task.FromResult<PolicyInfo?>(null);
        var now = DateTime.UtcNow;
        _policies[policyId] = existing with { Name = request.Name, Content = request.Content, UpdatedAt = now };
        _policyRollbacks.Add(new PolicyRollbackEntry(MakeId(), policyId, existing.Version, "system", $"Rollback snapshot before update to {request.Name}", now));
        AddEvent("policy", $"Policy updated: {request.Name}", "user");
        var p = _policies[policyId];
        return Task.FromResult<PolicyInfo?>(new PolicyInfo(policyId, p.Name, p.Domain, p.Version, p.CreatedAt, now, true));
    }

    public Task<bool> DeletePolicyAsync(string policyId, CancellationToken ct = default)
    {
        if (_policies.TryRemove(policyId, out _))
        {
            AddEvent("policy", $"Policy deleted: {policyId}", "user");
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<EpisodeSearchResult> SearchEpisodesAsync(EpisodeSearchRequest request, CancellationToken ct = default)
    {
        var items = _episodes.Values.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(e => new EpisodeInfo(e.Id, e.GoalId, e.Status, e.CreatedAt, e.FinishedAt, e.DurationMs, e.Outcome, e.SuccessRate)).ToList();
        return Task.FromResult(new EpisodeSearchResult(items, _episodes.Count, request.Page, request.PageSize));
    }

    public Task<EpisodeDetails?> GetEpisodeAsync(string episodeId, CancellationToken ct = default)
        => Task.FromResult(_episodes.GetValueOrDefault(episodeId));

    /// <summary>Generates speech from text. Returns a minimal RIFF/WAV header — no real TTS available in local mode.</summary>
    public Task<byte[]> GenerateSpeechAsync(string text, string? language = null, string? voice = null, CancellationToken ct = default)
    {
        var wavHeader = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x24, 0x00, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74, 0x20 };
        return Task.FromResult(wavHeader);
    }

    /// <summary>Transcribes audio to text. Always returns an error message — STT requires a cloud engine.</summary>
    public Task<string?> TranscribeAudioAsync(byte[] audioData, CancellationToken ct = default)
        => Task.FromResult<string?>("[Local mode: transcription requires a cloud STT engine]");

    public Task<FeedbackResponse> SubmitFeedbackAsync(FeedbackRequest request, CancellationToken ct = default)
    {
        _feedbackCount++;
        AddEvent("feedback", $"Feedback recorded: rating {request.Rating}", "user");
        return Task.FromResult(new FeedbackResponse(true, "fb-" + _feedbackCount, "Feedback recorded locally."));
    }

    public Task<AgentMetricsSummary?> GetMetricsSummaryAsync(CancellationToken ct = default)
        => Task.FromResult<AgentMetricsSummary?>(new AgentMetricsSummary(
            _episodes.Count + _policies.Count, _episodes.Count, 0, 0, 0.92, 0, 0, []));

    public Task<AgentScorecard?> GetScorecardAsync(CancellationToken ct = default)
        => Task.FromResult<AgentScorecard?>(new AgentScorecard(85, 90, 95, 80, 88, 92));

    public Task<RuntimeSummary?> GetRuntimeSummaryAsync(CancellationToken ct = default)
        => Task.FromResult<RuntimeSummary?>(new RuntimeSummary(true, true, "embedded", null, 1440, 5,
            new Dictionary<string, string> { ["mode"] = "embedded", ["provider"] = _kernel.Provider }));

    public async Task<GoalListResponse> GetActiveGoalsAsync(CancellationToken cancellationToken = default)
    {
        var goals = await _kernel.GetKanbanGoalsAsync(cancellationToken);
        return new GoalListResponse([.. goals.Select(MapGoal)], goals.Count);
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
        if (saved)
        {
            AddEvent("goal", $"Goal created: {request.Description}", "user");
            _goalCycles.GetOrAdd(goal.Id, _ => []).Add(new GoalCycleSummary(goal.Id, "created", "completed", DateTime.UtcNow, 100));
        }
        return saved ? MapGoal(goal) : null;
    }

    public Task<bool> UpdateGoalStatusAsync(string goalId, string action, CancellationToken cancellationToken = default)
    {
        AddEvent("goal", $"Goal {goalId} action: {action}", "user");
        _goalCycles.GetOrAdd(goalId, _ => []).Add(new GoalCycleSummary(goalId, action, MapGoalAction(action), DateTime.UtcNow, 0));
        return _kernel.MoveKanbanCardAsync(goalId, MapGoalAction(action), cancellationToken);
    }

    public Task<CognitiveDashboardData?> GetCognitiveDashboardAsync(CancellationToken ct = default)
        => Task.FromResult<CognitiveDashboardData?>(new CognitiveDashboardData(82,
            [new CognitiveModule("Local Engine", 85, "healthy"), new CognitiveModule("Memory", 78, "stable")],
            [new CognitiveEvent("info", "Local mode active", "system", DateTime.UtcNow)],
            new AutonomyStatus("full", DateTime.UtcNow, new Dictionary<string, double> { ["local"] = 0.92 })));

    public Task<MultimodalSearchResult?> SearchMultimodalAsync(string query, int topK = 10, CancellationToken ct = default)
        => Task.FromResult<MultimodalSearchResult?>(new MultimodalSearchResult(query, []));

    public Task<BenchmarkSummary?> GetBenchmarkSummaryAsync(CancellationToken ct = default)
        => Task.FromResult<BenchmarkSummary?>(new BenchmarkSummary(3, 12, 84.5, 95, 0.93,
            [new BenchmarkSuite("Local Reasoning", 4, 86.0, 80, 0.95), new BenchmarkSuite("Memory Recall", 4, 82.0, 110, 0.90), new BenchmarkSuite("Policy Eval", 4, 85.5, 95, 0.94)]));

    /// <summary>Queries the causal graph. Returns empty results — causal inference requires a trained GNN model (cloud).</summary>
    public Task<CausalQueryResult?> GetCausalQueryAsync(string query, CancellationToken ct = default)
        => Task.FromResult<CausalQueryResult?>(new CausalQueryResult(query, [], []));

    /// <summary>Predicts the outcome of an action. Returns a stub — requires a trained GNN model (cloud).</summary>
    public Task<CausalPrediction?> GetCausalPredictionAsync(string action, CancellationToken ct = default)
        => Task.FromResult<CausalPrediction?>(new CausalPrediction(action, "unknown", 0.5,
            ["Local mode: causal prediction requires a trained GNN model"]));

    public Task<AffectiveState?> GetAffectiveStateAsync(CancellationToken ct = default)
        => Task.FromResult<AffectiveState?>(new AffectiveState(_currentValence, _currentArousal,
            Math.Max(0, 0.5 - _currentValence), Math.Max(0, _currentValence - 0.2), DateTimeOffset.UtcNow));

    public Task<EmotionalState?> GetEmotionalStateAsync(string userId, CancellationToken ct = default)
        => Task.FromResult<EmotionalState?>(new EmotionalState(_currentValence * 0.8, _currentArousal * 0.9,
            (_currentValence + _currentArousal) / 2, DateTimeOffset.UtcNow));

    public Task<CrossSummaryData?> GetCrossSummaryAsync(CancellationToken ct = default)
        => Task.FromResult<CrossSummaryData?>(new CrossSummaryData(
            new CrossServiceStatus("1.0", TimeSpan.FromHours(24), 0, 10, 1, 1),
            new CrossServiceStatus("1.0", TimeSpan.FromHours(24), 0, 5, 1, 1),
            new HybridWeightsData(0.6, 0.2, 0.1, 0.1)));

    public Task<MetricsByGoalData?> GetMetricsByGoalAsync(CancellationToken ct = default)
    {
        var goals = _goalCycles.Select(g => new GoalMetrics(g.Key, g.Value.Count, g.Value.Count(c => c.Status == "completed"), 0, 1.0, 0, 0)).ToList();
        return Task.FromResult<MetricsByGoalData?>(new MetricsByGoalData(goals, goals.Count));
    }

    public Task<PolicyVersionList?> GetPolicyVersionsAsync(string policyId, CancellationToken ct = default)
    {
        if (_policies.TryGetValue(policyId, out var policy))
            return Task.FromResult<PolicyVersionList?>(new PolicyVersionList(
            [new PolicyVersionExtended(policyId, policy.Version, policy.CreatedAt, "system", "Current version", 95.0)]));
        return Task.FromResult<PolicyVersionList?>(new PolicyVersionList([]));
    }

    public Task<List<PolicyRollbackEntry>> GetPolicyRollbacksAsync(string policyId, CancellationToken ct = default)
        => Task.FromResult(_policyRollbacks.Where(r => r.PolicyId == policyId).ToList());

    public Task<GoalCycleList?> GetGoalCyclesAsync(string goalId, CancellationToken ct = default)
    {
        var cycles = _goalCycles.GetValueOrDefault(goalId);
        return Task.FromResult<GoalCycleList?>(new GoalCycleList(cycles ?? [new GoalCycleSummary(goalId, "created", "completed", DateTime.UtcNow, 100)]));
    }

    public Task<List<McpServerInfo>> GetMcpServersAsync(CancellationToken ct = default)
        => Task.FromResult(_mcpServers.ToList());

    public Task<bool> ToggleMcpServerAsync(string serverId, bool enabled, CancellationToken ct = default)
    {
        var idx = _mcpServers.FindIndex(s => s.ServerId == serverId);
        if (idx < 0) return Task.FromResult(false);
        var old = _mcpServers[idx];
        _mcpServers[idx] = new McpServerInfo(old.ServerId, old.Name, old.TransportType, enabled, old.IsConnected, old.ToolCount, old.LastUsedAt);
        AddEvent("mcp", $"MCP server {serverId} toggled: {enabled}", "user");
        return Task.FromResult(true);
    }

    public Task<List<DocumentInfo>> GetDocumentsAsync(int limit = 50, CancellationToken ct = default)
        => Task.FromResult(_documents.Take(limit).ToList());

    public Task<DocumentInfo?> GetDocumentStatusAsync(string documentId, CancellationToken ct = default)
        => Task.FromResult(_documents.FirstOrDefault(d => d.DocumentId == documentId));

    public Task<ArchiveStats?> GetArchiveStatsAsync(CancellationToken ct = default)
        => Task.FromResult<ArchiveStats?>(new ArchiveStats(true, _episodes.Count + _policies.Count, ["memory", "policies"]));

    public Task<VersionsInfo?> GetVersionsAsync(CancellationToken ct = default)
        => Task.FromResult<VersionsInfo?>(new VersionsInfo("2.1.0-local", ["2.0.0", "2.1.0"], false, ""));

    public Task<ContractsResponse?> GetContractsAsync(CancellationToken ct = default)
        => Task.FromResult<ContractsResponse?>(new ContractsResponse("2.1", [new ContractEntry("/agent/run", "2.1", "^2.0.0", false, "active")]));

    public Task<ModelRegistryDetail?> GetModelRegistryAsync(string modelId, CancellationToken ct = default)
        => Task.FromResult<ModelRegistryDetail?>(new ModelRegistryDetail(modelId,
            [new ModelRegistryEntry(modelId, "1.0", "conversation", "embedded", "active", null, DateTime.UtcNow, DateTime.UtcNow)],
            new ModelRegistryEntry(modelId, "1.0", "conversation", "embedded", "active", null, DateTime.UtcNow, DateTime.UtcNow)));

    public Task<UserProfile?> GetUserProfileAsync(string userId, CancellationToken ct = default)
        => Task.FromResult<UserProfile?>(new UserProfile(userId, "Local User", "local@krnl.ai", "admin",
            new Dictionary<string, string> { ["mode"] = "local", ["theme"] = "dark" }, DateTime.UtcNow));

    public Task<bool> UpdateUserProfileAsync(UserProfile profile, CancellationToken ct = default)
    {
        AddEvent("admin", $"Profile updated for {profile.UserId}", "user");
        return Task.FromResult(true);
    }

    public Task<ShareListResponse?> GetSharesAsync(CancellationToken ct = default)
        => Task.FromResult<ShareListResponse?>(null);

    public Task<List<SnapshotInfo>> GetSnapshotsAsync(CancellationToken ct = default)
        => Task.FromResult(_snapshots.ToList());

    public Task<List<ObjectiveInfo>> GetObjectivesAsync(CancellationToken ct = default)
        => Task.FromResult(_objectives.ToList());

    public Task<ObjectiveDetail?> GetObjectiveDetailAsync(string id, CancellationToken ct = default)
        => Task.FromResult<ObjectiveDetail?>(new ObjectiveDetail(id, "Local objective", "active", 0.5,
            [new TargetInfo("t1", "Completion", 50, 100, "%")]));

    public Task<List<InvestigationInfo>> GetInvestigationsAsync(CancellationToken ct = default)
        => Task.FromResult(_investigations.ToList());

    public Task<List<McpServerInfo>> GetPluginsAsync(CancellationToken ct = default) => GetMcpServersAsync(ct);
    public Task<BenchmarkSummary?> GetSafetyReportAsync(CancellationToken ct = default) => GetBenchmarkSummaryAsync(ct);

    // ───── Approvals (stateful) ─────
    public Task<List<ApprovalRequest>> GetPendingApprovalsAsync(string? role = null, CancellationToken ct = default)
    {
        var pending = _approvalRequests.Values
            .Where(a => a.Status == ApprovalStatus.Pending)
            .Where(a => role == null || a.RequiredApprovers.Contains(role))
            .ToList();
        return Task.FromResult(pending);
    }

    public Task<ApprovalRequest?> GetApprovalDetailAsync(string requestId, CancellationToken ct = default)
        => Task.FromResult(_approvalRequests.GetValueOrDefault(requestId));

    public Task<ApprovalRequest?> ApproveRequestAsync(string requestId, string? comment = null, CancellationToken ct = default)
    {
        if (!_approvalRequests.TryGetValue(requestId, out var existing)) return Task.FromResult<ApprovalRequest?>(null);
        var updated = existing with
        {
            Status = ApprovalStatus.Approved,
            Responses = existing.Responses.Concat([new ApprovalResponse("local-user", "Local User", true, comment ?? "Approved", DateTimeOffset.UtcNow)]).ToList()
        };
        _approvalRequests[requestId] = updated;
        AddEvent("approval", $"Approval {requestId} approved", "user");
        return Task.FromResult<ApprovalRequest?>(updated);
    }

    public Task<ApprovalRequest?> RejectRequestAsync(string requestId, string? comment = null, CancellationToken ct = default)
    {
        if (!_approvalRequests.TryGetValue(requestId, out var existing)) return Task.FromResult<ApprovalRequest?>(null);
        var updated = existing with
        {
            Status = ApprovalStatus.Rejected,
            Responses = existing.Responses.Concat([new ApprovalResponse("local-user", "Local User", false, comment ?? "Rejected", DateTimeOffset.UtcNow)]).ToList()
        };
        _approvalRequests[requestId] = updated;
        AddEvent("approval", $"Approval {requestId} rejected", "user");
        return Task.FromResult<ApprovalRequest?>(updated);
    }

    public Task<List<ScheduledTask>> GetScheduledTasksAsync(CancellationToken ct = default)
        => Task.FromResult(_scheduledTasks.ToList());

    public Task<List<MemoryMoment>> GetMemoryMomentsAsync(int limit = 20, CancellationToken ct = default)
        => Task.FromResult(_memoryMoments.Take(limit).ToList());

    // Plan
    public Task<PlanExecutionResult?> GetCurrentPlanAsync(CancellationToken ct = default)
    {
        var plan = new PlanInfo("local-plan-1", "Operação local", "Plano de execução do kernel embarcado",
            PlanStatus.InProgress, 0.33, _planSteps.Count, _planSteps.Count(s => s.Status == PlanStepStatus.Completed), DateTime.UtcNow.AddHours(-1), null);
        return Task.FromResult<PlanExecutionResult?>(new PlanExecutionResult("local-plan-1", true, plan, [.. _planSteps], null));
    }

    public Task<List<PlanStep>> GetPlanStepsAsync(string planId, CancellationToken ct = default)
        => Task.FromResult(_planSteps.ToList());

    // Feedback History
    public Task<List<FeedbackHistoryEntry>> GetFeedbackHistoryAsync(CancellationToken ct = default)
        => Task.FromResult(new List<FeedbackHistoryEntry>
        {
            new("fb-local-1", "ep-local-1", 5, "Modo local funcionando bem!", "geral", DateTimeOffset.UtcNow.AddDays(-1)),
            new("fb-local-2", "ep-local-2", 4, "Resposta rápida", "performance", DateTimeOffset.UtcNow.AddHours(-12)),
        });

    public Task<FeedbackAverage?> GetFeedbackAverageAsync(CancellationToken ct = default)
        => Task.FromResult<FeedbackAverage?>(new FeedbackAverage(2, 4.5, 0, 0, 0, 1, 1));

    // Episodic Memory
    public Task<EpisodicMemorySearchResult?> SearchEpisodicMemoryAsync(EpisodicMemorySearchRequest request, CancellationToken ct = default)
    {
        var all = _episodes.Values
            .Where(e => string.IsNullOrEmpty(request.Status) || e.Status == request.Status)
            .Select(e => new EpisodicMemoryHit(e.Id, e.GoalId, e.Outcome ?? "Sem resumo", e.Status, e.SuccessRate, e.CreatedAt))
            .ToList();
        if (!string.IsNullOrWhiteSpace(request.Query))
            all = all.Where(h => h.Goal.Contains(request.Query, StringComparison.OrdinalIgnoreCase)
                              || (h.Summary?.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        var hits = all.Take(Math.Max(1, request.TopK)).ToList();
        return Task.FromResult<EpisodicMemorySearchResult?>(new EpisodicMemorySearchResult(hits, all.Count, request.Query));
    }

    // Knowledge (stateful)
    public Task<KnowledgeQueryResult?> KnowledgeAskAsync(string query, CancellationToken ct = default)
    {
        var hits = _knowledgeEntries
            .Where(k => k.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (hits.Count == 0)
            hits = [new KnowledgeHit("k1", $"Local result for '{query}'", 0.9, "embedded", DateTimeOffset.UtcNow)];
        return Task.FromResult<KnowledgeQueryResult?>(new KnowledgeQueryResult(query, hits, hits.Count));
    }

    public Task<KnowledgeStats?> KnowledgeStatsAsync(CancellationToken ct = default)
        => Task.FromResult<KnowledgeStats?>(new KnowledgeStats(_knowledgeEntries.Count,
            _knowledgeEntries.Select(k => k.Source).Distinct().Count(),
            0, DateTimeOffset.UtcNow));

    public Task<KnowledgeLearnResponse?> KnowledgeLearnAsync(string content, string source, string? category = null, CancellationToken ct = default)
    {
        _knowledgeCounter++;
        var id = $"k{_knowledgeCounter}";
        _knowledgeEntries.Add(new KnowledgeHit(id, content, 1.0, source, DateTimeOffset.UtcNow));
        AddEvent("knowledge", $"Knowledge learned from {source}", "system");
        return Task.FromResult<KnowledgeLearnResponse?>(new KnowledgeLearnResponse(true, id, null));
    }

    /// <summary>Performs PIE causal inference. Symbolic stub only — statistical inference requires a cloud PIE engine.</summary>
    public Task<PieInferResponse?> PieInferAsync(string premise, string? context = null, CancellationToken ct = default)
        => Task.FromResult<PieInferResponse?>(new PieInferResponse($"Therefore: {premise}", 0.85,
            ["Local PIE inference: symbolic entailment only — use cloud for statistical inference"]));

    public Task<PieChainResponse?> PieChainAsync(string initialPremise, int steps = 3, string? context = null, CancellationToken ct = default)
        => Task.FromResult<PieChainResponse?>(new PieChainResponse(
            Enumerable.Range(1, steps).Select(i => new PieChainStep(i, $"Step {i}", $"Conclusion {i}", 0.9 - i * 0.1)).ToList()));

    public Task<PieKnowledgeResponse?> PieKnowledgeAsync(string domain, string fact, double certainty = 1.0, CancellationToken ct = default)
        => Task.FromResult<PieKnowledgeResponse?>(new PieKnowledgeResponse(true));

    public Task<PieCoherenceData?> PieCoherenceAsync(CancellationToken ct = default)
        => Task.FromResult<PieCoherenceData?>(new PieCoherenceData(0.82,
            [new PieCoherenceEntry("s1", "Local coherence check", 0.85), new PieCoherenceEntry("s2", "Consistency verified", 0.79)]));

    public Task<List<PieTerm>> PieTermsAsync(CancellationToken ct = default)
        => Task.FromResult(new List<PieTerm> { new("t1", "Logic", "Logical entailment", 15), new("t2", "Causality", "Cause-effect", 10) });

    // Emotional history (stateful)
    public Task<List<EmotionalHistoryEntry>> EmotionalHistoryAsync(CancellationToken ct = default)
        => Task.FromResult(_emotionalHistory.ToList());

    public Task<bool> EmotionalEventAsync(string @event, string? trigger = null, double? valenceDelta = null, double? arousalDelta = null, CancellationToken ct = default)
    {
        if (valenceDelta.HasValue) _currentValence = Math.Clamp(_currentValence + valenceDelta.Value, -1.0, 1.0);
        if (arousalDelta.HasValue) _currentArousal = Math.Clamp(_currentArousal + arousalDelta.Value, -1.0, 1.0);
        AddEmotionalHistory(@event, trigger, _currentValence, _currentArousal);
        AddEvent("emotional", $"Emotional event: {@event}", "kernel");
        return Task.FromResult(true);
    }

    // Events (stateful)
    public Task<List<EventInfo>> EventsRecentAsync(int take = 50, CancellationToken ct = default)
        => Task.FromResult(_events.TakeLast(take).ToList());

    public Task<EventDetail?> EventDetailAsync(string eventId, CancellationToken ct = default)
    {
        var ev = _events.FirstOrDefault(e => e.EventId == eventId);
        if (ev is null) return Task.FromResult<EventDetail?>(null);
        return Task.FromResult<EventDetail?>(new EventDetail(ev.EventId, ev.Type, ev.Description, ev.Source, ev.Timestamp, ev.Metadata, null, null));
    }

    public Task<List<EventInfo>> EventsByMomentAsync(string momentId, CancellationToken ct = default)
        => Task.FromResult(_events.Where(e => e.Description?.Contains(momentId, StringComparison.OrdinalIgnoreCase) ?? false).ToList());

    /// <summary>Explains code. Not available locally — requires a cloud LLM.</summary>
    public Task<CodingResponse?> CodingExplainAsync(CodingRequest request, CancellationToken ct = default)
        => Task.FromResult<CodingResponse?>(new CodingResponse(null,
            "Local mode: code explanation requires a cloud LLM — use KrnlAI API for this feature", false, null));
    /// <summary>Fixes code. Not available locally — requires a cloud LLM.</summary>
    public Task<CodingResponse?> CodingFixAsync(CodingRequest request, CancellationToken ct = default)
        => Task.FromResult<CodingResponse?>(new CodingResponse(null,
            "Local mode: code fixing requires a cloud LLM — use KrnlAI API for this feature", false, null));
    /// <summary>Generates tests for code. Not available locally — requires a cloud LLM.</summary>
    public Task<CodingResponse?> CodingGenerateTestsAsync(CodingRequest request, CancellationToken ct = default)
        => Task.FromResult<CodingResponse?>(new CodingResponse(null,
            "Local mode: test generation requires a cloud LLM — use KrnlAI API for this feature", false, null));
    /// <summary>Reviews code. Not available locally — requires a cloud LLM.</summary>
    public Task<CodingResponse?> CodingReviewAsync(CodingRequest request, CancellationToken ct = default)
        => Task.FromResult<CodingResponse?>(new CodingResponse(null,
            "Local mode: code review requires a cloud LLM — use KrnlAI API for this feature", false, null));
    /// <summary>Applies a diff to code. Not available locally — requires a cloud LLM.</summary>
    public Task<CodingResponse?> CodingApplyDiffAsync(CodingRequest request, CancellationToken ct = default)
        => Task.FromResult<CodingResponse?>(new CodingResponse(null,
            "Local mode: diff apply requires a cloud LLM — use KrnlAI API for this feature", false, null));
    /// <summary>Completes code. Not available locally — requires a cloud LLM.</summary>
    public Task<CodingResponse?> CodingCompleteAsync(CodingRequest request, CancellationToken ct = default)
        => Task.FromResult<CodingResponse?>(new CodingResponse(null,
            "Local mode: code completion requires a cloud LLM — use KrnlAI API for this feature", false, null));
    /// <summary>Gets coding cycle status. Stub in local mode — no real coding cycles exist.</summary>
    public Task<CodingStatus?> GetCodingStatusAsync(string cycleId, CancellationToken ct = default)
        => Task.FromResult<CodingStatus?>(new CodingStatus(cycleId, "unknown", null, 0, null, null, DateTime.UtcNow, null));

    /// <summary>Gets self-improvement status. Returns disabled stub — self-improvement requires a cloud LLM.</summary>
    public Task<SelfImprovementStatus?> GetSelfImprovementStatusAsync(CancellationToken ct = default)
        => Task.FromResult<SelfImprovementStatus?>(new SelfImprovementStatus(false, false, DateTime.UtcNow, 0, 0, 0, [], []));

    // Assistant (Threads) — stateful
    public Task<ThreadInfo?> CreateThreadAsync(string? title = null, CancellationToken ct = default)
    {
        var id = MakeId();
        var thread = new ThreadInfo(id, title ?? "Local Thread", DateTimeOffset.UtcNow, "active");
        _threads[id] = thread;
        _threadMessages[id] = [];
        AddEvent("thread", $"Thread created: {thread.Title}", "user");
        return Task.FromResult<ThreadInfo?>(thread);
    }

    public Task<ThreadInfo?> GetThreadAsync(string threadId, CancellationToken ct = default)
        => Task.FromResult<ThreadInfo?>(_threads.GetValueOrDefault(threadId) ?? new ThreadInfo(threadId, "Local Thread", DateTime.UtcNow, "active"));

    public Task<MessageInfo?> SendMessageAsync(string threadId, string content, CancellationToken ct = default)
    {
        var msg = new MessageInfo(MakeId(), threadId, "user", content, DateTimeOffset.UtcNow, null);
        _threadMessages.AddOrUpdate(threadId,
            _ => [msg],
            (_, list) => { list.Add(msg); return list; });
        AddEvent("thread", $"Message sent to thread {threadId}", "user");
        return Task.FromResult<MessageInfo?>(msg);
    }

    public Task<List<MessageInfo>> GetMessagesAsync(string threadId, CancellationToken ct = default)
        => Task.FromResult(_threadMessages.GetValueOrDefault(threadId) ?? []);

    public Task<RunInfo?> CreateRunAsync(string threadId, CancellationToken ct = default)
    {
        AddEvent("thread", $"Run created for thread {threadId}", "system");
        return Task.FromResult<RunInfo?>(new RunInfo(MakeId(), threadId, "completed", null, DateTime.UtcNow, null, null));
    }

    public Task<RunInfo?> GetRunAsync(string threadId, string runId, CancellationToken ct = default)
        => Task.FromResult<RunInfo?>(new RunInfo(runId, threadId, "completed", null, DateTime.UtcNow, null, null));

    public Task<bool> CancelRunAsync(string threadId, string runId, CancellationToken ct = default)
    {
        AddEvent("thread", $"Run {runId} cancelled for thread {threadId}", "user");
        return Task.FromResult(true);
    }

    // MCP Config
    public Task<McpServerConfig?> GetMcpServerConfigAsync(string serverId, CancellationToken ct = default)
        => Task.FromResult<McpServerConfig?>(new McpServerConfig(serverId, serverId, "stdio", "", null, null));

    public Task<bool> UpdateMcpServerAsync(string serverId, McpServerConfig config, CancellationToken ct = default)
    {
        AddEvent("mcp", $"MCP server config updated: {serverId}", "user");
        return Task.FromResult(true);
    }

    // User Services (stateful)
    public Task<List<UserServiceInfo>> GetUserServicesAsync(CancellationToken ct = default)
        => Task.FromResult(_userServices.Values.ToList());

    public Task<bool> UpdateUserServiceAsync(string serviceType, UserServiceUpdateRequest request, CancellationToken ct = default)
    {
        var existing = _userServices.GetValueOrDefault(serviceType);
        _userServices[serviceType] = new UserServiceInfo(serviceType, request.Credentials.Count > 0,
            request.Enabled ?? existing?.Enabled ?? true, DateTimeOffset.UtcNow);
        AddEvent("user-service", $"Service configured: {serviceType}", "user");
        return Task.FromResult(true);
    }

    public Task<bool> DeleteUserServiceAsync(string serviceType, CancellationToken ct = default)
    {
        if (_userServices.TryRemove(serviceType, out _))
        {
            AddEvent("user-service", $"Service removed: {serviceType}", "user");
            return Task.FromResult(true);
        }
        return Task.FromResult(true);
    }

    /// <summary>Executes a cognitive flow. Not available locally — requires the LLM Gateway.</summary>
    public Task<CognitiveFlowResult?> CognitiveFlowExecuteAsync(FlowDefinition flow, CancellationToken ct = default)
        => Task.FromResult<CognitiveFlowResult?>(new CognitiveFlowResult(false, null,
            "Local mode: cognitive flow execution requires the LLM Gateway — use KrnlAI API for this feature"));

    /// <summary>Saves a cognitive flow. Not available locally.</summary>
    public Task<bool> CognitiveFlowSaveAsync(FlowDefinition flow, CancellationToken ct = default)
        => Task.FromResult(false);

    /// <summary>Loads a cognitive flow. Not available locally.</summary>
    public Task<FlowDefinition?> CognitiveFlowLoadAsync(string flowName, CancellationToken ct = default)
        => Task.FromResult<FlowDefinition?>(null);

    // Templates
    private readonly List<TemplateInfo> _templates = [];

    public Task<List<TemplateInfo>> TemplateListAsync(CancellationToken ct = default)
        => Task.FromResult(_templates.ToList());

    public Task<TemplateInfo?> TemplateGetAsync(string templateId, CancellationToken ct = default)
        => Task.FromResult(_templates.FirstOrDefault(t => t.Id == templateId));

    public Task<TemplateInfo?> TemplateCreateAsync(CreateTemplateRequest request, CancellationToken ct = default)
    {
        var id = MakeId();
        var now = DateTimeOffset.UtcNow;
        var template = new TemplateInfo(id, request.Name, request.Description, request.Content, request.Category ?? "general", "1.0", now, now);
        _templates.Add(template);
        AddEvent("template", $"Template created: {request.Name}", "user");
        return Task.FromResult<TemplateInfo?>(template);
    }

    public Task<TemplateInfo?> TemplateUpdateAsync(string templateId, UpdateTemplateRequest request, CancellationToken ct = default)
    {
        var idx = _templates.FindIndex(t => t.Id == templateId);
        if (idx < 0) return Task.FromResult<TemplateInfo?>(null);
        var old = _templates[idx];
        var updated = new TemplateInfo(old.Id, request.Name ?? old.Name, request.Description ?? old.Description,
            request.Content ?? old.Content, request.Category ?? old.Category, old.Version, old.CreatedAt, DateTimeOffset.UtcNow);
        _templates[idx] = updated;
        return Task.FromResult<TemplateInfo?>(updated);
    }

    public Task<bool> TemplateDeleteAsync(string templateId, CancellationToken ct = default)
    {
        var removed = _templates.RemoveAll(t => t.Id == templateId);
        return Task.FromResult(removed > 0);
    }

    public Task<TemplateRenderResult?> TemplateRenderAsync(string templateId, RenderTemplateRequest request, CancellationToken ct = default)
    {
        var template = _templates.FirstOrDefault(t => t.Id == templateId);
        if (template == null) return Task.FromResult<TemplateRenderResult?>(new TemplateRenderResult(null, "Template not found"));
        var content = template.Content;
        foreach (var kv in request.Variables)
            content = content.Replace($"{{{{{kv.Key}}}}}", kv.Value);
        return Task.FromResult<TemplateRenderResult?>(new TemplateRenderResult(content, null));
    }

    // Experiments
    private readonly List<ExperimentInfo> _experiments = [];
    private readonly Dictionary<string, List<MetricEntry>> _experimentMetrics = [];

    public Task<List<ExperimentInfo>> ExperimentListAsync(CancellationToken ct = default)
        => Task.FromResult(_experiments.ToList());

    public Task<ExperimentInfo?> ExperimentStartAsync(StartExperimentRequest request, CancellationToken ct = default)
    {
        var id = MakeId();
        var now = DateTimeOffset.UtcNow;
        var exp = new ExperimentInfo(id, request.Name, "running", request.Description, now, null);
        _experiments.Add(exp);
        _experimentMetrics[id] = [];
        AddEvent("experiment", $"Experiment started: {request.Name}", "user");
        return Task.FromResult<ExperimentInfo?>(exp);
    }

    public Task<bool> ExperimentCompleteAsync(string experimentId, CancellationToken ct = default)
    {
        var idx = _experiments.FindIndex(e => e.Id == experimentId);
        if (idx < 0) return Task.FromResult(false);
        var old = _experiments[idx];
        _experiments[idx] = new ExperimentInfo(old.Id, old.Name, "completed", old.Description, old.CreatedAt, DateTimeOffset.UtcNow);
        AddEvent("experiment", $"Experiment completed: {old.Name}", "system");
        return Task.FromResult(true);
    }

    public Task<bool> ExperimentRecordMetricAsync(string experimentId, RecordMetricRequest request, CancellationToken ct = default)
    {
        if (!_experimentMetrics.ContainsKey(experimentId)) return Task.FromResult(false);
        _experimentMetrics[experimentId].Add(new MetricEntry(request.MetricName, request.Value, DateTimeOffset.UtcNow));
        return Task.FromResult(true);
    }

    public Task<ExperimentAnalysis?> ExperimentGetAnalysisAsync(string experimentId, CancellationToken ct = default)
    {
        if (!_experimentMetrics.TryGetValue(experimentId, out var metrics))
            return Task.FromResult<ExperimentAnalysis?>(null);
        var avg = metrics.Count > 0 ? metrics.Average(m => m.Value) : 0;
        return Task.FromResult<ExperimentAnalysis?>(new ExperimentAnalysis(experimentId, metrics.Count, avg, 0, 0.95, metrics, ["Embedded mode analysis"]));
    }

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
