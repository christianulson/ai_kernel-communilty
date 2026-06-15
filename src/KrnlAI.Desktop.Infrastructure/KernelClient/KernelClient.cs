using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Infrastructure.Abstractions;
using CoreModels = KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Infrastructure.KernelClient;

public class KernelClient : IKernelClient
{
    private readonly IGatewayApi _api;
    private readonly AuthTokenProvider _tokenProvider;

    public KernelClient(IGatewayApi api, AuthTokenProvider tokenProvider)
    {
        _api = api;
        _tokenProvider = tokenProvider;
    }

    public void SetAuthToken(string? token) => _tokenProvider.Token = token;
    public void SetTokens(string? token, string? refreshToken) => _tokenProvider.SetTokens(token, refreshToken);
    public void SetBaseUrl(string baseUrl) => DynamicBaseUrlHandler.SetBaseUrl(baseUrl);

    public Task<CoreModels.AgentRunResponse> RunAgentAsync(CoreModels.AgentRunRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.RunAgentAsync(request, ct);
            return new CoreModels.AgentRunResponse(r.Narration, r.Command,
                r.TransportSteps?.Select(t => new CoreModels.TransportStep(t.Label, t.Detail, t.Ok, t.Status)).ToList(),
                r.ActiveStages, r.Error);
        }, new CoreModels.AgentRunResponse(null, null, null, null, null));

    public Task<byte[]> GenerateSpeechAsync(string text, string? language = null, string? voice = null, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GenerateSpeechAsync(new Core.Models.SpeechRequest(text, language ?? "pt-BR", voice), ct);
            return !string.IsNullOrEmpty(r.Base64) ? Convert.FromBase64String(r.Base64) : [];
        }, []);

    public Task<bool> CheckHealthAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => (await _api.GetHealthAsync(ct)).IsHealthy, false);

    public Task<string?> TranscribeAudioAsync(byte[] audioData, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.TranscribeAudioAsync(new Core.Models.TranscribeRequest(Convert.ToBase64String(audioData), "pt"), ct);
            return r.Text;
        }, default(string?));

    public Task<CoreModels.LoginResponse> LoginAsync(CoreModels.LoginRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.LoginAsync(request, ct);
            return new CoreModels.LoginResponse(
                Success: !string.IsNullOrEmpty(r.Token),
                Token: r.Token,
                Username: r.User?.Email,
                RefreshToken: r.RefreshToken
            );
        }, new CoreModels.LoginResponse(false));

    public Task<CoreModels.PolicyListResponse> GetPoliciesAsync(string? domain = null, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetPoliciesAsync(page, pageSize, domain, ct);
            return new CoreModels.PolicyListResponse(
                r.Policies?.Select(p => new CoreModels.PolicyInfo(p.Id, p.Name, p.Domain, p.Version, p.CreatedAt, p.UpdatedAt, p.IsActive)).ToList() ?? [],
                r.TotalCount, r.Page, r.PageSize);
        }, new CoreModels.PolicyListResponse([], 0, page, pageSize));

    public Task<CoreModels.PolicyDetails?> GetPolicyAsync(string policyId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetPolicyAsync(policyId, ct);
            return new CoreModels.PolicyDetails(r.Id, r.Name, r.Domain, r.Version, r.Content ?? "", r.CreatedAt, r.UpdatedAt, r.IsActive,
                r.Versions?.Select(v => new CoreModels.PolicyVersion(v.Version, v.CreatedAt, v.CreatedBy, v.ChangeNote)).ToList());
        }, default(CoreModels.PolicyDetails?));

    public Task<CoreModels.PolicyInfo?> CreatePolicyAsync(CoreModels.CreatePolicyRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.CreatePolicyAsync(request, ct);
            return new CoreModels.PolicyInfo(r.Id, r.Name, r.Domain, r.Version, r.CreatedAt, r.UpdatedAt, r.IsActive);
        }, default(CoreModels.PolicyInfo?));

    public Task<CoreModels.PolicyInfo?> UpdatePolicyAsync(string policyId, CoreModels.UpdatePolicyRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.UpdatePolicyAsync(policyId, request, ct);
            return new CoreModels.PolicyInfo(r.Id, r.Name, r.Domain, r.Version, r.CreatedAt, r.UpdatedAt, r.IsActive);
        }, default(CoreModels.PolicyInfo?));

    public Task<bool> DeletePolicyAsync(string policyId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => { await _api.DeletePolicyAsync(policyId, ct); return true; }, false);

    public Task<CoreModels.EpisodeSearchResult> SearchEpisodesAsync(CoreModels.EpisodeSearchRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.SearchEpisodesAsync(request.Query, request.GoalId, request.Status,
                request.FromDate?.ToUniversalTime(), request.ToDate?.ToUniversalTime(), request.Page, request.PageSize, ct);
            return new CoreModels.EpisodeSearchResult(
                r.Episodes?.Select(e => new CoreModels.EpisodeInfo(e.Id, e.GoalId, e.Status, e.CreatedAt, e.FinishedAt, e.DurationMs, e.Outcome, e.SuccessRate)).ToList() ?? [],
                r.TotalCount, r.Page, r.PageSize);
        }, new CoreModels.EpisodeSearchResult([], 0, request.Page, request.PageSize));

    public Task<CoreModels.EpisodeDetails?> GetEpisodeAsync(string episodeId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetEpisodeAsync(episodeId, ct);
            return new CoreModels.EpisodeDetails(r.Id, r.GoalId, r.Status, r.CreatedAt, r.FinishedAt, r.DurationMs, r.Outcome, r.SuccessRate, r.Summary,
                r.Steps?.Select(s => new CoreModels.EpisodeStep(s.StepIndex, s.Label, s.Detail, s.StartedAt, s.FinishedAt, s.DurationMs, s.Ok, s.Error)).ToList());
        }, default(CoreModels.EpisodeDetails?));

    public Task<CoreModels.MemorySearchResult> SearchMemoryAsync(string query, int topK = 10, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.SearchMemoryAsync(new MemorySearchRequestDto(query, topK), ct);
            return new CoreModels.MemorySearchResult(
                r.Hits?.Select(h => new CoreModels.MemoryHit(h.Id, h.Content, h.Source, h.Score, h.CreatedAt, h.Metadata)).ToList() ?? [],
                r.TotalCount, r.QueryTimeMs);
        }, new CoreModels.MemorySearchResult([], 0, 0));

    public Task<CoreModels.MemoryIngestResult> IngestMemoryAsync(CoreModels.MemoryIngestRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.IngestMemoryAsync(request, ct);
            return new CoreModels.MemoryIngestResult(r.Success, r.DocumentId, r.ChunksCreated, r.Error);
        }, new CoreModels.MemoryIngestResult(false, null, 0, null));

    public Task<CoreModels.MemoryMetrics?> GetMemoryMetricsAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetMemoryMetricsAsync(ct);
            return new CoreModels.MemoryMetrics(r.TotalChunks, r.TotalDocuments, r.TotalSizeBytes,
                r.BySource?.ToDictionary(kv => kv.Key, kv => kv.Value) ?? [], r.OldestEntry, r.NewestEntry);
        }, default(CoreModels.MemoryMetrics?));

    public Task<CoreModels.WorkingMemorySummary?> GetWorkingMemoryAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetWorkingMemoryAsync(ct);
            return new CoreModels.WorkingMemorySummary(r.ActiveSlots, r.MaxSlots,
                r.Slots?.Select(s => new CoreModels.WorkingMemorySlot(s.Key, s.Content, s.Relevance, s.CreatedAt, s.ExpiresAt)).ToList() ?? []);
        }, default(CoreModels.WorkingMemorySummary?));

    public Task<CoreModels.AgentMetricsSummary?> GetMetricsSummaryAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetMetricsSummaryAsync(ct);
            return new CoreModels.AgentMetricsSummary(r.TotalRuns, r.CompletedRuns, r.FailedRuns, r.AbortedRuns, r.SuccessRate, r.AvgLatencyMs, r.AvgCost,
                r.ByGoal?.ToDictionary(kv => kv.Key, kv => new CoreModels.GoalMetrics(kv.Key, kv.Value.TotalRuns, kv.Value.CompletedRuns, kv.Value.FailedRuns, kv.Value.SuccessRate, kv.Value.AvgLatencyMs, kv.Value.AvgEstimatedCost)) ?? []);
        }, default(CoreModels.AgentMetricsSummary?));

    public Task<CoreModels.AgentScorecard?> GetScorecardAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetScorecardAsync(ct);
            return new CoreModels.AgentScorecard(r.Reliability, r.Efficiency, r.Safety, r.AntiLoop, r.Governance, r.Overall);
        }, default(CoreModels.AgentScorecard?));

    public Task<CoreModels.RuntimeSummary?> GetRuntimeSummaryAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetRuntimeSummaryAsync(ct);
            return new CoreModels.RuntimeSummary(r.GatewayHealthy, r.KernelHealthy, r.KernelVersion, r.GatewayVersion, r.ActiveGoals, r.MemoryUsageBytes, r.Services ?? []);
        }, default(CoreModels.RuntimeSummary?));

    public Task<CoreModels.GoalListResponse> GetActiveGoalsAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetActiveGoalsAsync(ct);
            return new CoreModels.GoalListResponse(
                r.Goals?.Select(g => new CoreModels.GoalInfo(g.GoalId, g.Description, g.Status, g.Priority, g.CreatedAt, g.CompletedAt, g.Deadline, g.SuccessRate, g.SubGoalCount, g.CompletedSubGoals)).ToList() ?? [],
                r.TotalCount);
        }, new CoreModels.GoalListResponse([], 0));

    public Task<CoreModels.GoalDetails?> GetGoalAsync(string goalId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetGoalAsync(goalId, ct);
            return new CoreModels.GoalDetails(r.GoalId, r.Description, r.Status, r.Priority, r.CreatedAt, r.CompletedAt, r.Deadline, r.SuccessRate,
                r.SubGoals?.Select(s => new CoreModels.SubGoal(s.Id, s.Description, s.Completed)).ToList(),
                r.Cycles?.Select(c => new CoreModels.GoalCycle(c.Action, c.Status, c.DurationMs, c.Timestamp, c.GoalId)).ToList());
        }, default(CoreModels.GoalDetails?));

    public Task<CoreModels.GoalInfo?> CreateGoalAsync(CoreModels.CreateGoalRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.CreateGoalAsync(request, ct);
            return new CoreModels.GoalInfo(r.GoalId, r.Description, r.Status, r.Priority, r.CreatedAt, r.CompletedAt, r.Deadline, r.SuccessRate, r.SubGoalCount, r.CompletedSubGoals);
        }, default(CoreModels.GoalInfo?));

    private static readonly HashSet<string> _allowedGoalActions = ["pause", "resume", "complete", "decompose"];

    public Task<bool> UpdateGoalStatusAsync(string goalId, string action, CancellationToken ct = default)
    {
        if (!_allowedGoalActions.Contains(action)) return Task.FromResult(false);
        return SafeCall.ExecuteAsync(async () => { await _api.UpdateGoalStatusAsync(goalId, action, ct); return true; }, false);
    }

    public Task<CoreModels.FeedbackResponse> SubmitFeedbackAsync(CoreModels.FeedbackRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.SubmitFeedbackAsync(request, ct);
            return new CoreModels.FeedbackResponse(r.Success, r.FeedbackId, r.Message);
        }, new CoreModels.FeedbackResponse(false, null, null));

    public Task<CoreModels.CognitiveDashboardData?> GetCognitiveDashboardAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetCognitiveDashboardAsync(ct);
            return new CoreModels.CognitiveDashboardData(r.OverallHealth,
                r.ActiveModules?.Select(m => new CoreModels.CognitiveModule(m.Name, m.HealthScore, m.Status)).ToList() ?? [],
                r.RecentEvents?.Select(e => new CoreModels.CognitiveEvent(e.Type, e.Description, e.Source, e.Timestamp)).ToList() ?? [],
                r.Autonomy != null ? new CoreModels.AutonomyStatus(r.Autonomy.Level, r.Autonomy.LastUpdated, r.Autonomy.DomainConfidence) : null);
        }, default(CoreModels.CognitiveDashboardData?));

    public Task<CoreModels.UserProfile?> GetUserProfileAsync(string userId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetUserProfileAsync(userId, ct);
            return new CoreModels.UserProfile(r.UserId, r.Name, r.Email, r.Role, r.Preferences, r.CreatedAt);
        }, default(CoreModels.UserProfile?));

    public Task<bool> UpdateUserProfileAsync(CoreModels.UserProfile profile, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => { await _api.UpdateUserProfileAsync(profile, ct); return true; }, false);

    public Task<CoreModels.MultimodalSearchResult?> SearchMultimodalAsync(string query, int topK = 10, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.SearchMultimodalAsync(new Core.Models.MultimodalSearchRequest(query, topK), ct);
            return new CoreModels.MultimodalSearchResult(query,
                r.Hits?.Select(h => new CoreModels.MultimodalHit(h.Id, h.Content, h.Modality, h.Score, h.ThumbnailBase64)).ToList() ?? []);
        }, default(CoreModels.MultimodalSearchResult?));

    public Task<CoreModels.BenchmarkSummary?> GetBenchmarkSummaryAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetBenchmarkSummaryAsync(ct);
            return new CoreModels.BenchmarkSummary(r.TotalSuites, r.TotalScenarios, r.OverallScore, r.AvgLatencyMs, r.AvgSuccessRate,
                r.Suites?.Select(s => new CoreModels.BenchmarkSuite(s.Name, s.Scenarios, s.Score, s.LatencyMs, s.SuccessRate)).ToList() ?? []);
        }, default(CoreModels.BenchmarkSummary?));

    public Task<CoreModels.CausalQueryResult?> GetCausalQueryAsync(string query, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetCausalCausesAsync(query, ct);
            return new CoreModels.CausalQueryResult(query,
                r.Nodes?.Select(n => new CoreModels.CausalNode(n.Id, n.Label, n.Type, n.Attributes)).ToList() ?? [],
                r.Edges?.Select(e => new CoreModels.CausalEdge(e.SourceId, e.TargetId, e.Label, e.Weight)).ToList() ?? []);
        }, default(CoreModels.CausalQueryResult?));

    public Task<CoreModels.CausalPrediction?> GetCausalPredictionAsync(string action, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetCausalPredictAsync(action, ct);
            return new CoreModels.CausalPrediction(r.Action, r.Outcome, r.Probability, r.ContributingFactors);
        }, default(CoreModels.CausalPrediction?));

    public Task<CoreModels.AffectiveState?> GetAffectiveStateAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetAffectiveStateAsync(ct);
            return new CoreModels.AffectiveState(r.Valence, r.Arousal, r.PainLevel, r.RewardLevel, r.UpdatedAt);
        }, default(CoreModels.AffectiveState?));

    public Task<CoreModels.EmotionalState?> GetEmotionalStateAsync(string userId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetEmotionalStateAsync(userId, ct);
            return new CoreModels.EmotionalState(r.Valence, r.Arousal, r.Motivation, r.UpdatedAt);
        }, default(CoreModels.EmotionalState?));

    public Task<CoreModels.CrossSummaryData?> GetCrossSummaryAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetCrossSummaryAsync(ct);
            return new CoreModels.CrossSummaryData(
                new CoreModels.CrossServiceStatus(r.Gateway?.Version, r.Gateway?.Uptime, r.Gateway?.ActiveLimiters ?? 0, r.Gateway?.RequestsPerMinute ?? 0, 0, 0),
                new CoreModels.CrossServiceStatus(r.Kernel?.Version, r.Kernel?.Uptime, 0, 0, r.Kernel?.ConnectedStores ?? 0, r.Kernel?.TotalStores ?? 0),
                r.HybridWeights != null ? new CoreModels.HybridWeightsData(r.HybridWeights.Semantic, r.HybridWeights.Lexical, r.HybridWeights.Recency, r.HybridWeights.Confidence) : null);
        }, default(CoreModels.CrossSummaryData?));

    public Task<CoreModels.MetricsByGoalData?> GetMetricsByGoalAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetMetricsByGoalAsync(ct);
            return new CoreModels.MetricsByGoalData(
                r.Goals?.Select(g => new CoreModels.GoalMetrics(g.GoalId, g.TotalRuns, g.CompletedRuns, g.FailedRuns, g.SuccessRate, g.AvgLatencyMs, g.AvgEstimatedCost)).ToList() ?? [],
                r.TotalCount);
        }, default(CoreModels.MetricsByGoalData?));

    public Task<CoreModels.PolicyVersionList?> GetPolicyVersionsAsync(string policyId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetPolicyVersionsAsync(policyId, ct);
            return new CoreModels.PolicyVersionList(
                r.Versions?.Select(v => new CoreModels.PolicyVersionExtended(v.PolicyId, v.Version, v.CreatedAt, v.CreatedBy, v.ChangeNote, v.SuccessRate)).ToList() ?? []);
        }, default(CoreModels.PolicyVersionList?));

    public Task<List<CoreModels.PolicyRollbackEntry>> GetPolicyRollbacksAsync(string policyId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetPolicyRollbacksAsync(policyId, ct);
            return r.Select(x => new CoreModels.PolicyRollbackEntry(x.RollbackId, x.PolicyId, x.TargetVersion, x.PerformedBy, x.Reason, x.PerformedAt)).ToList();
        }, new List<CoreModels.PolicyRollbackEntry>());

    public Task<CoreModels.GoalCycleList?> GetGoalCyclesAsync(string goalId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetGoalCyclesAsync(goalId, ct);
            return new CoreModels.GoalCycleList(r.Select(c => new CoreModels.GoalCycleSummary(c.GoalId, c.Action, c.Status, c.Timestamp, c.DurationMs)).ToList());
        }, default(CoreModels.GoalCycleList?));

    public Task<List<CoreModels.McpServerInfo>> GetMcpServersAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetMcpServersAsync(ct);
            return r.Select(s => new CoreModels.McpServerInfo(s.ServerId, s.Name, s.TransportType, s.Enabled, s.IsConnected, s.ToolCount, s.LastUsedAt)).ToList();
        }, new List<CoreModels.McpServerInfo>());

    public Task<bool> ToggleMcpServerAsync(string serverId, bool enabled, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => { await _api.ToggleMcpServerAsync(serverId, new McpToggleRequest(enabled), ct); return true; }, false);

    public Task<List<Core.Models.DocumentInfo>> GetDocumentsAsync(int limit = 50, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetDocumentsAsync(limit, ct);
            return r.Select(d => new Core.Models.DocumentInfo(d.DocumentId, d.FileName, d.FileSize, d.Format, d.Status, d.ErrorMessage, d.ChunkCount, d.CreatedAt, d.CompletedAt)).ToList();
        }, new List<Core.Models.DocumentInfo>());

    public Task<Core.Models.DocumentInfo?> GetDocumentStatusAsync(string documentId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var d = await _api.GetDocumentStatusAsync(documentId, ct);
            return new Core.Models.DocumentInfo(d.DocumentId, d.FileName, d.FileSize, d.Format, d.Status, d.ErrorMessage, d.ChunkCount, d.CreatedAt, d.CompletedAt);
        }, default(Core.Models.DocumentInfo?));

    public Task<Core.Models.ArchiveStats?> GetArchiveStatsAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetArchiveStatsAsync(ct);
            return new Core.Models.ArchiveStats(r.Ok, r.TotalArchived, r.Stores ?? []);
        }, default(Core.Models.ArchiveStats?));

    public Task<Core.Models.VersionsInfo?> GetVersionsAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetVersionsAsync(ct);
            return new Core.Models.VersionsInfo(r.DefaultVersion, r.SupportedVersions ?? [], r.LegacyUnversionedDeprecated, r.LegacySunsetDate);
        }, default(Core.Models.VersionsInfo?));

    public Task<Core.Models.ContractsResponse?> GetContractsAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetContractsAsync(ct);
            var contracts = r.Contracts?.Select(c => new Core.Models.ContractEntry(c.Endpoint, c.ContractVersion, c.SupportedRange, c.Deprecated, c.State)).ToList() ?? [];
            return new Core.Models.ContractsResponse(r.DefaultApiVersion, contracts);
        }, default(Core.Models.ContractsResponse?));

    public Task<Core.Models.ModelRegistryDetail?> GetModelRegistryAsync(string modelId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetModelRegistryAsync(modelId, ct);
            var models = r.Models?.Select(m => new Core.Models.ModelRegistryEntry(m.ModelId, m.ModelVersion, m.UseCase, m.Runtime, m.Status, m.ApprovedBy, m.CreatedAt, m.ActivatedAt)).ToList() ?? [];
            Core.Models.ModelRegistryEntry? active = r.Active != null ? new Core.Models.ModelRegistryEntry(r.Active.ModelId, r.Active.ModelVersion, r.Active.UseCase, r.Active.Runtime, r.Active.Status, r.Active.ApprovedBy, r.Active.CreatedAt, r.Active.ActivatedAt) : null;
            return new Core.Models.ModelRegistryDetail(r.ModelId, models, active);
        }, default(Core.Models.ModelRegistryDetail?));

    public Task<Core.Models.ShareListResponse?> GetSharesAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetSharesAsync(ct);
            var shares = r.Shares?.Select(s => new Core.Models.SessionShare(s.ShareCode, s.SessionId, s.AccessLevel, s.CreatedAt, s.ExpiresAt, s.AccessCount, s.IsRevoked)).ToList() ?? [];
            return new Core.Models.ShareListResponse(shares);
        }, default(Core.Models.ShareListResponse?));

    public Task<List<Core.Models.SnapshotInfo>> GetSnapshotsAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetSnapshotsAsync(ct);
            return r.Select(s => new Core.Models.SnapshotInfo(s.SnapshotId, s.Label, s.CreatedAt, s.Size)).ToList();
        }, []);

    public Task<List<Core.Models.ObjectiveInfo>> GetObjectivesAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetObjectivesAsync(ct);
            return r.Select(o => new Core.Models.ObjectiveInfo(o.ObjectiveId, o.Description, o.Status, o.Progress, o.Priority, o.Deadline)).ToList();
        }, []);

    public Task<Core.Models.ObjectiveDetail?> GetObjectiveDetailAsync(string id, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetObjectiveDetailAsync(id, ct);
            return new Core.Models.ObjectiveDetail(r.ObjectiveId, r.Description, r.Status, r.Progress,
                r.Targets?.Select(t => new Core.Models.TargetInfo(t.TargetId, t.Description, t.CurrentValue, t.TargetValue, t.Unit)).ToList() ?? []);
        }, default(Core.Models.ObjectiveDetail?));

    public Task<List<Core.Models.InvestigationInfo>> GetInvestigationsAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetInvestigationsAsync(ct);
            return r.Select(i => new Core.Models.InvestigationInfo(i.CaseId, i.Title, i.Status, i.EvidenceCount, i.CreatedAt)).ToList();
        }, []);

    public Task<List<Core.Models.McpServerInfo>> GetPluginsAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetMcpServersAsync(ct);
            return r.Select(s => new Core.Models.McpServerInfo(s.ServerId, s.Name, s.TransportType, s.Enabled, s.IsConnected, s.ToolCount, s.LastUsedAt)).ToList();
        }, []);

    public Task<Core.Models.BenchmarkSummary?> GetSafetyReportAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await _api.GetBenchmarkSummaryAsync(ct);
            return new Core.Models.BenchmarkSummary(r.TotalSuites, r.TotalScenarios, r.OverallScore, r.AvgLatencyMs, r.AvgSuccessRate,
                r.Suites?.Select(s => new Core.Models.BenchmarkSuite(s.Name, s.Scenarios, s.Score, s.LatencyMs, s.SuccessRate)).ToList() ?? []);
        }, default(Core.Models.BenchmarkSummary?));

    public Task<List<Core.Models.ScheduledTask>> GetScheduledTasksAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new List<Core.Models.ScheduledTask>(), []);

    public Task<List<Core.Models.MemoryMoment>> GetMemoryMomentsAsync(int limit = 20, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new List<Core.Models.MemoryMoment>(), []);
}
