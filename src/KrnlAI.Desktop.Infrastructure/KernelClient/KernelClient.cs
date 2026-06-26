using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Infrastructure.Abstractions;
using Cts = KrnlAI.Contracts;
using CoreModels = KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Infrastructure.KernelClient;

public class KernelClient(IGatewayApi api, AuthTokenProvider tokenProvider) : IKernelClient
{
    public void SetAuthToken(string? token) => tokenProvider.Token = token;
    public void SetTokens(string? token, string? refreshToken) => tokenProvider.SetTokens(token, refreshToken);
    public void SetBaseUrl(string baseUrl) => DynamicBaseUrlHandler.SetBaseUrl(baseUrl);

    public Task<Cts.AgentRunTransportResponse> RunAgentAsync(Cts.AgentRunTransportRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.RunAgentAsync(request, ct).ConfigureAwait(false);
            return new Cts.AgentRunTransportResponse(r.Narration, r.Command,
                r.TransportSteps?.Select(t => new Cts.TransportStepDto(t.Label, t.Detail, t.Ok, t.Status)).ToList(),
                r.ActiveStages, r.Error);
        }, new Cts.AgentRunTransportResponse(null, null, null, null, null));

    public Task<byte[]> GenerateSpeechAsync(string text, string? language = null, string? voice = null, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GenerateSpeechAsync(new CoreModels.SpeechRequest(text, language ?? "pt-BR", voice), ct).ConfigureAwait(false);
            return !string.IsNullOrEmpty(r.Base64) ? Convert.FromBase64String(r.Base64) : [];
        }, []);

    public Task<bool> CheckHealthAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => (await api.GetHealthAsync(ct).ConfigureAwait(false)).IsHealthy, false);

    public Task<string?> TranscribeAudioAsync(byte[] audioData, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.TranscribeAudioAsync(new CoreModels.TranscribeRequest(Convert.ToBase64String(audioData), "pt"), ct).ConfigureAwait(false);
            return r.Text;
        }, default(string?));

    public Task<CoreModels.LoginResponse> LoginAsync(CoreModels.LoginRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.LoginAsync(request, ct).ConfigureAwait(false);
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
            var r = await api.GetPoliciesAsync(page, pageSize, domain, ct).ConfigureAwait(false);
            return new CoreModels.PolicyListResponse(
                r.Policies?.Select(p => new CoreModels.PolicyInfo(p.Id, p.Name, p.Domain, p.Version, p.CreatedAt, p.UpdatedAt, p.IsActive)).ToList() ?? [],
                r.TotalCount, r.Page, r.PageSize);
        }, new CoreModels.PolicyListResponse([], 0, page, pageSize));

    public Task<CoreModels.PolicyDetails?> GetPolicyAsync(string policyId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetPolicyAsync(policyId, ct).ConfigureAwait(false);
            return new CoreModels.PolicyDetails(r.Id, r.Name, r.Domain, r.Version, r.Content ?? "", r.CreatedAt, r.UpdatedAt, r.IsActive,
                r.Versions?.Select(v => new CoreModels.PolicyVersion(v.Version, v.CreatedAt, v.CreatedBy, v.ChangeNote)).ToList());
        }, default(CoreModels.PolicyDetails?));

    public Task<CoreModels.PolicyInfo?> CreatePolicyAsync(CoreModels.CreatePolicyRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.CreatePolicyAsync(request, ct).ConfigureAwait(false);
            return new CoreModels.PolicyInfo(r.Id, r.Name, r.Domain, r.Version, r.CreatedAt, r.UpdatedAt, r.IsActive);
        }, default(CoreModels.PolicyInfo?));

    public Task<CoreModels.PolicyInfo?> UpdatePolicyAsync(string policyId, CoreModels.UpdatePolicyRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.UpdatePolicyAsync(policyId, request, ct).ConfigureAwait(false);
            return new CoreModels.PolicyInfo(r.Id, r.Name, r.Domain, r.Version, r.CreatedAt, r.UpdatedAt, r.IsActive);
        }, default(CoreModels.PolicyInfo?));

    public Task<bool> DeletePolicyAsync(string policyId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => { await api.DeletePolicyAsync(policyId, ct).ConfigureAwait(false); return true; }, false);

    public Task<CoreModels.EpisodeSearchResult> SearchEpisodesAsync(CoreModels.EpisodeSearchRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.SearchEpisodesAsync(request.Query, request.GoalId, request.Status,
                request.FromDate?.ToUniversalTime(), request.ToDate?.ToUniversalTime(), request.Page, request.PageSize, ct).ConfigureAwait(false);
            return new CoreModels.EpisodeSearchResult(
                r.Episodes?.Select(e => new CoreModels.EpisodeInfo(e.Id, e.GoalId, e.Status, e.CreatedAt, e.FinishedAt, e.DurationMs, e.Outcome, e.SuccessRate)).ToList() ?? [],
                r.TotalCount, r.Page, r.PageSize);
        }, new CoreModels.EpisodeSearchResult([], 0, request.Page, request.PageSize));

    public Task<CoreModels.EpisodeDetails?> GetEpisodeAsync(string episodeId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetEpisodeAsync(episodeId, ct).ConfigureAwait(false);
            return new CoreModels.EpisodeDetails(r.Id, r.GoalId, r.Status, r.CreatedAt, r.FinishedAt, r.DurationMs, r.Outcome, r.SuccessRate, r.Summary,
                r.Steps?.Select(s => new CoreModels.EpisodeStep(s.StepIndex, s.Label, s.Detail, s.StartedAt, s.FinishedAt, s.DurationMs, s.Ok, s.Error)).ToList());
        }, default(CoreModels.EpisodeDetails?));

    public Task<CoreModels.MemorySearchResult> SearchMemoryAsync(string query, int topK = 10, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.SearchMemoryAsync(new MemorySearchRequestDto(query, topK), ct).ConfigureAwait(false);
            return new CoreModels.MemorySearchResult(
                r.Hits?.Select(h => new CoreModels.MemoryHit(h.Id, h.Content, h.Source, h.Score, h.CreatedAt, h.Metadata)).ToList() ?? [],
                r.TotalCount, r.QueryTimeMs);
        }, new CoreModels.MemorySearchResult([], 0, 0));

    public Task<CoreModels.MemoryIngestResult> IngestMemoryAsync(CoreModels.MemoryIngestRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.IngestMemoryAsync(request, ct).ConfigureAwait(false);
            return new CoreModels.MemoryIngestResult(r.Success, r.DocumentId, r.ChunksCreated, r.Error);
        }, new CoreModels.MemoryIngestResult(false, null, 0, null));

    public Task<CoreModels.MemoryMetrics?> GetMemoryMetricsAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetMemoryMetricsAsync(ct).ConfigureAwait(false);
            return new CoreModels.MemoryMetrics(r.TotalChunks, r.TotalDocuments, r.TotalSizeBytes,
                r.BySource?.ToDictionary(kv => kv.Key, kv => kv.Value) ?? [], r.OldestEntry, r.NewestEntry);
        }, default(CoreModels.MemoryMetrics?));

    public Task<CoreModels.WorkingMemorySummary?> GetWorkingMemoryAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetWorkingMemoryAsync(ct).ConfigureAwait(false);
            return new CoreModels.WorkingMemorySummary(r.ActiveSlots, r.MaxSlots,
                r.Slots?.Select(s => new CoreModels.WorkingMemorySlot(s.Key, s.Content, s.Relevance, s.CreatedAt, s.ExpiresAt)).ToList() ?? []);
        }, default(CoreModels.WorkingMemorySummary?));

    public Task<CoreModels.AgentMetricsSummary?> GetMetricsSummaryAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetMetricsSummaryAsync(ct).ConfigureAwait(false);
            return new CoreModels.AgentMetricsSummary(r.TotalRuns, r.CompletedRuns, r.FailedRuns, r.AbortedRuns, r.SuccessRate, r.AvgLatencyMs, r.AvgCost,
                r.ByGoal?.ToDictionary(kv => kv.Key, kv => new CoreModels.GoalMetrics(kv.Key, kv.Value.TotalRuns, kv.Value.CompletedRuns, kv.Value.FailedRuns, kv.Value.SuccessRate, kv.Value.AvgLatencyMs, kv.Value.AvgEstimatedCost)) ?? []);
        }, default(CoreModels.AgentMetricsSummary?));

    public Task<CoreModels.AgentScorecard?> GetScorecardAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetScorecardAsync(ct).ConfigureAwait(false);
            return new CoreModels.AgentScorecard(r.Reliability, r.Efficiency, r.Safety, r.AntiLoop, r.Governance, r.Overall);
        }, default(CoreModels.AgentScorecard?));

    public Task<CoreModels.RuntimeSummary?> GetRuntimeSummaryAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetRuntimeSummaryAsync(ct).ConfigureAwait(false);
            return new CoreModels.RuntimeSummary(r.GatewayHealthy, r.KernelHealthy, r.KernelVersion, r.GatewayVersion, r.ActiveGoals, r.MemoryUsageBytes, r.Services ?? []);
        }, default(CoreModels.RuntimeSummary?));

    public Task<CoreModels.GoalListResponse> GetActiveGoalsAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetActiveGoalsAsync(ct).ConfigureAwait(false);
            return new CoreModels.GoalListResponse(
                r.Goals?.Select(g => new CoreModels.GoalInfo(g.GoalId, g.Description, g.Status, g.Priority, g.CreatedAt, g.CompletedAt, g.Deadline, g.SuccessRate, g.SubGoalCount, g.CompletedSubGoals)).ToList() ?? [],
                r.TotalCount);
        }, new CoreModels.GoalListResponse([], 0));

    public Task<CoreModels.GoalDetails?> GetGoalAsync(string goalId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetGoalAsync(goalId, ct).ConfigureAwait(false);
            return new CoreModels.GoalDetails(r.GoalId, r.Description, r.Status, r.Priority, r.CreatedAt, r.CompletedAt, r.Deadline, r.SuccessRate,
                r.SubGoals?.Select(s => new CoreModels.SubGoal(s.Id, s.Description, s.Completed)).ToList(),
                r.Cycles?.Select(c => new CoreModels.GoalCycle(c.Action, c.Status, c.DurationMs, c.Timestamp, c.GoalId)).ToList());
        }, default(CoreModels.GoalDetails?));

    public Task<CoreModels.GoalInfo?> CreateGoalAsync(CoreModels.CreateGoalRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.CreateGoalAsync(request, ct).ConfigureAwait(false);
            return new CoreModels.GoalInfo(r.GoalId, r.Description, r.Status, r.Priority, r.CreatedAt, r.CompletedAt, r.Deadline, r.SuccessRate, r.SubGoalCount, r.CompletedSubGoals);
        }, default(CoreModels.GoalInfo?));

    private static readonly HashSet<string> _allowedGoalActions = ["pause", "resume", "complete", "decompose"];

    public Task<bool> UpdateGoalStatusAsync(string goalId, string action, CancellationToken ct = default)
    {
        if (!_allowedGoalActions.Contains(action)) return Task.FromResult(false);
        return SafeCall.ExecuteAsync(async () => { await api.UpdateGoalStatusAsync(goalId, action, ct).ConfigureAwait(false); return true; }, false);
    }

    public Task<CoreModels.FeedbackResponse> SubmitFeedbackAsync(CoreModels.FeedbackRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.SubmitFeedbackAsync(request, ct).ConfigureAwait(false);
            return new CoreModels.FeedbackResponse(r.Success, r.FeedbackId, r.Message);
        }, new CoreModels.FeedbackResponse(false, null, null));

    public Task<CoreModels.CognitiveDashboardData?> GetCognitiveDashboardAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetCognitiveDashboardAsync(ct).ConfigureAwait(false);
            return new CoreModels.CognitiveDashboardData(r.OverallHealth,
                r.ActiveModules?.Select(m => new CoreModels.CognitiveModule(m.Name, m.HealthScore, m.Status)).ToList() ?? [],
                r.RecentEvents?.Select(e => new CoreModels.CognitiveEvent(e.Type, e.Description, e.Source, e.Timestamp)).ToList() ?? [],
                r.Autonomy != null ? new CoreModels.AutonomyStatus(r.Autonomy.Level, r.Autonomy.LastUpdated, r.Autonomy.DomainConfidence) : null);
        }, default(CoreModels.CognitiveDashboardData?));

    public Task<CoreModels.UserProfile?> GetUserProfileAsync(string userId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetUserProfileAsync(userId, ct).ConfigureAwait(false);
            return new CoreModels.UserProfile(r.UserId, r.Name, r.Email, r.Role, r.Preferences, r.CreatedAt);
        }, default(CoreModels.UserProfile?));

    public Task<bool> UpdateUserProfileAsync(CoreModels.UserProfile profile, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => { await api.UpdateUserProfileAsync(profile, ct).ConfigureAwait(false); return true; }, false);

    public Task<CoreModels.MultimodalSearchResult?> SearchMultimodalAsync(string query, int topK = 10, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.SearchMultimodalAsync(new CoreModels.MultimodalSearchRequest(query, topK), ct).ConfigureAwait(false);
            return new CoreModels.MultimodalSearchResult(query,
                r.Hits?.Select(h => new CoreModels.MultimodalHit(h.Id, h.Content, h.Modality, h.Score, h.ThumbnailBase64)).ToList() ?? []);
        }, default(CoreModels.MultimodalSearchResult?));

    public Task<CoreModels.BenchmarkSummary?> GetBenchmarkSummaryAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetBenchmarkSummaryAsync(ct).ConfigureAwait(false);
            return new CoreModels.BenchmarkSummary(r.TotalSuites, r.TotalScenarios, r.OverallScore, r.AvgLatencyMs, r.AvgSuccessRate,
                r.Suites?.Select(s => new CoreModels.BenchmarkSuite(s.Name, s.Scenarios, s.Score, s.LatencyMs, s.SuccessRate)).ToList() ?? []);
        }, default(CoreModels.BenchmarkSummary?));

    public Task<CoreModels.CausalQueryResult?> GetCausalQueryAsync(string query, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetCausalCausesAsync(query, ct).ConfigureAwait(false);
            return new CoreModels.CausalQueryResult(query,
                r.Nodes?.Select(n => new CoreModels.CausalNode(n.Id, n.Label, n.Type, n.Attributes)).ToList() ?? [],
                r.Edges?.Select(e => new CoreModels.CausalEdge(e.SourceId, e.TargetId, e.Label, e.Weight)).ToList() ?? []);
        }, default(CoreModels.CausalQueryResult?));

    public Task<CoreModels.CausalPrediction?> GetCausalPredictionAsync(string action, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetCausalPredictAsync(action, ct).ConfigureAwait(false);
            return new CoreModels.CausalPrediction(r.Action, r.Outcome, r.Probability, r.ContributingFactors);
        }, default(CoreModels.CausalPrediction?));

    public Task<CoreModels.AffectiveState?> GetAffectiveStateAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetAffectiveStateAsync(ct).ConfigureAwait(false);
            return new CoreModels.AffectiveState(r.Valence, r.Arousal, r.PainLevel, r.RewardLevel, r.UpdatedAt);
        }, default(CoreModels.AffectiveState?));

    public Task<CoreModels.EmotionalState?> GetEmotionalStateAsync(string userId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetEmotionalStateAsync(userId, ct).ConfigureAwait(false);
            return new CoreModels.EmotionalState(r.Valence, r.Arousal, r.Motivation, r.UpdatedAt);
        }, default(CoreModels.EmotionalState?));

    public Task<CoreModels.CrossSummaryData?> GetCrossSummaryAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetCrossSummaryAsync(ct).ConfigureAwait(false);
            return new CoreModels.CrossSummaryData(
                new CoreModels.CrossServiceStatus(r.Gateway?.Version, r.Gateway?.Uptime, r.Gateway?.ActiveLimiters ?? 0, r.Gateway?.RequestsPerMinute ?? 0, 0, 0),
                new CoreModels.CrossServiceStatus(r.Kernel?.Version, r.Kernel?.Uptime, 0, 0, r.Kernel?.ConnectedStores ?? 0, r.Kernel?.TotalStores ?? 0),
                r.HybridWeights != null ? new CoreModels.HybridWeightsData(r.HybridWeights.Semantic, r.HybridWeights.Lexical, r.HybridWeights.Recency, r.HybridWeights.Confidence) : null);
        }, default(CoreModels.CrossSummaryData?));

    public Task<CoreModels.MetricsByGoalData?> GetMetricsByGoalAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetMetricsByGoalAsync(ct).ConfigureAwait(false);
            return new CoreModels.MetricsByGoalData(
                r.Goals?.Select(g => new CoreModels.GoalMetrics(g.GoalId, g.TotalRuns, g.CompletedRuns, g.FailedRuns, g.SuccessRate, g.AvgLatencyMs, g.AvgEstimatedCost)).ToList() ?? [],
                r.TotalCount);
        }, default(CoreModels.MetricsByGoalData?));

    public Task<CoreModels.PolicyVersionList?> GetPolicyVersionsAsync(string policyId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetPolicyVersionsAsync(policyId, ct).ConfigureAwait(false);
            return new CoreModels.PolicyVersionList(
                r.Versions?.Select(v => new CoreModels.PolicyVersionExtended(v.PolicyId, v.Version, v.CreatedAt, v.CreatedBy, v.ChangeNote, v.SuccessRate)).ToList() ?? []);
        }, default(CoreModels.PolicyVersionList?));

    public Task<List<CoreModels.PolicyRollbackEntry>> GetPolicyRollbacksAsync(string policyId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetPolicyRollbacksAsync(policyId, ct).ConfigureAwait(false);
            return [.. r.Select(x => new CoreModels.PolicyRollbackEntry(x.RollbackId, x.PolicyId, x.TargetVersion, x.PerformedBy, x.Reason, x.PerformedAt))];
        }, new List<CoreModels.PolicyRollbackEntry>());

    public Task<CoreModels.GoalCycleList?> GetGoalCyclesAsync(string goalId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetGoalCyclesAsync(goalId, ct).ConfigureAwait(false);
            return new CoreModels.GoalCycleList([.. r.Select(c => new CoreModels.GoalCycleSummary(c.GoalId, c.Action, c.Status, c.Timestamp, c.DurationMs))]);
        }, default(CoreModels.GoalCycleList?));

    public Task<List<CoreModels.McpServerInfo>> GetMcpServersAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetMcpServersAsync(ct).ConfigureAwait(false);
            return [.. r.Select(s => new CoreModels.McpServerInfo(s.ServerId, s.Name, s.TransportType, s.Enabled, s.IsConnected, s.ToolCount, s.LastUsedAt))];
        }, new List<CoreModels.McpServerInfo>());

    public Task<bool> ToggleMcpServerAsync(string serverId, bool enabled, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => { await api.ToggleMcpServerAsync(serverId, new McpToggleRequest(enabled), ct).ConfigureAwait(false); return true; }, false);

    public Task<List<Core.Models.DocumentInfo>> GetDocumentsAsync(int limit = 50, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetDocumentsAsync(limit, ct).ConfigureAwait(false);
            return [.. r.Select(d => new Core.Models.DocumentInfo(d.DocumentId, d.FileName, d.FileSize, d.Format, d.Status, d.ErrorMessage, d.ChunkCount, d.CreatedAt, d.CompletedAt))];
        }, new List<Core.Models.DocumentInfo>());

    public Task<Core.Models.DocumentInfo?> GetDocumentStatusAsync(string documentId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var d = await api.GetDocumentStatusAsync(documentId, ct).ConfigureAwait(false);
            return new Core.Models.DocumentInfo(d.DocumentId, d.FileName, d.FileSize, d.Format, d.Status, d.ErrorMessage, d.ChunkCount, d.CreatedAt, d.CompletedAt);
        }, default(Core.Models.DocumentInfo?));

    public Task<Core.Models.ArchiveStats?> GetArchiveStatsAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetArchiveStatsAsync(ct).ConfigureAwait(false);
            return new Core.Models.ArchiveStats(r.Ok, r.TotalArchived, r.Stores ?? []);
        }, default(Core.Models.ArchiveStats?));

    public Task<Core.Models.VersionsInfo?> GetVersionsAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetVersionsAsync(ct).ConfigureAwait(false);
            return new Core.Models.VersionsInfo(r.DefaultVersion, r.SupportedVersions ?? [], r.LegacyUnversionedDeprecated, r.LegacySunsetDate);
        }, default(Core.Models.VersionsInfo?));

    public Task<Core.Models.ContractsResponse?> GetContractsAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetContractsAsync(ct).ConfigureAwait(false);
            var contracts = r.Contracts?.Select(c => new Core.Models.ContractEntry(c.Endpoint, c.ContractVersion, c.SupportedRange, c.Deprecated, c.State)).ToList() ?? [];
            return new Core.Models.ContractsResponse(r.DefaultApiVersion, contracts);
        }, default(Core.Models.ContractsResponse?));

    public Task<Core.Models.ModelRegistryDetail?> GetModelRegistryAsync(string modelId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetModelRegistryAsync(modelId, ct).ConfigureAwait(false);
            var models = r.Models?.Select(m => new Core.Models.ModelRegistryEntry(m.ModelId, m.ModelVersion, m.UseCase, m.Runtime, m.Status, m.ApprovedBy, m.CreatedAt, m.ActivatedAt)).ToList() ?? [];
            Core.Models.ModelRegistryEntry? active = r.Active != null ? new Core.Models.ModelRegistryEntry(r.Active.ModelId, r.Active.ModelVersion, r.Active.UseCase, r.Active.Runtime, r.Active.Status, r.Active.ApprovedBy, r.Active.CreatedAt, r.Active.ActivatedAt) : null;
            return new Core.Models.ModelRegistryDetail(r.ModelId, models, active);
        }, default(Core.Models.ModelRegistryDetail?));

    public Task<Core.Models.ShareListResponse?> GetSharesAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetSharesAsync(ct).ConfigureAwait(false);
            var shares = r.Shares?.Select(s => new Core.Models.SessionShare(s.ShareCode, s.SessionId, s.AccessLevel, s.CreatedAt, s.ExpiresAt, s.AccessCount, s.IsRevoked)).ToList() ?? [];
            return new Core.Models.ShareListResponse(shares);
        }, default(Core.Models.ShareListResponse?));

    public Task<List<Core.Models.SnapshotInfo>> GetSnapshotsAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetSnapshotsAsync(ct).ConfigureAwait(false);
            return r.Select(s => new Core.Models.SnapshotInfo(s.SnapshotId, s.Label, s.CreatedAt, s.Size)).ToList();
        }, []);

    public Task<List<Core.Models.ObjectiveInfo>> GetObjectivesAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetObjectivesAsync(ct).ConfigureAwait(false);
            return r.Select(o => new Core.Models.ObjectiveInfo(o.ObjectiveId, o.Description, o.Status, o.Progress, o.Priority, o.Deadline)).ToList();
        }, []);

    public Task<Core.Models.ObjectiveDetail?> GetObjectiveDetailAsync(string id, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetObjectiveDetailAsync(id, ct).ConfigureAwait(false);
            return new Core.Models.ObjectiveDetail(r.ObjectiveId, r.Description, r.Status, r.Progress,
                r.Targets?.Select(t => new Core.Models.TargetInfo(t.TargetId, t.Description, t.CurrentValue, t.TargetValue, t.Unit)).ToList() ?? []);
        }, default(Core.Models.ObjectiveDetail?));

    public Task<List<Core.Models.InvestigationInfo>> GetInvestigationsAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetInvestigationsAsync(ct).ConfigureAwait(false);
            return r.Select(i => new Core.Models.InvestigationInfo(i.CaseId, i.Title, i.Status, i.EvidenceCount, i.CreatedAt)).ToList();
        }, []);

    public Task<List<Core.Models.McpServerInfo>> GetPluginsAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetMcpServersAsync(ct).ConfigureAwait(false);
            return r.Select(s => new Core.Models.McpServerInfo(s.ServerId, s.Name, s.TransportType, s.Enabled, s.IsConnected, s.ToolCount, s.LastUsedAt)).ToList();
        }, []);

    public Task<Core.Models.BenchmarkSummary?> GetSafetyReportAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetBenchmarkSummaryAsync(ct).ConfigureAwait(false);
            return new Core.Models.BenchmarkSummary(r.TotalSuites, r.TotalScenarios, r.OverallScore, r.AvgLatencyMs, r.AvgSuccessRate,
                r.Suites?.Select(s => new Core.Models.BenchmarkSuite(s.Name, s.Scenarios, s.Score, s.LatencyMs, s.SuccessRate)).ToList() ?? []);
        }, default(Core.Models.BenchmarkSummary?));

    public Task<List<Core.Models.ScheduledTask>> GetScheduledTasksAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new List<Core.Models.ScheduledTask>(), []);

    public Task<List<Core.Models.MemoryMoment>> GetMemoryMomentsAsync(int limit = 20, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new List<Core.Models.MemoryMoment>(), []);

    // Knowledge
    public Task<CoreModels.KnowledgeQueryResult?> KnowledgeAskAsync(string query, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.KnowledgeAskAsync(query, ct).ConfigureAwait(false);
            return new CoreModels.KnowledgeQueryResult(r.Query,
                r.Hits?.Select(h => new CoreModels.KnowledgeHit(h.Id, h.Content, h.Score, h.Source, h.CreatedAt)).ToList() ?? [],
                r.TotalCount);
        }, default(CoreModels.KnowledgeQueryResult?));

    public Task<CoreModels.KnowledgeStats?> KnowledgeStatsAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.KnowledgeStatsAsync(ct).ConfigureAwait(false);
            return new CoreModels.KnowledgeStats(r.TotalEntries, r.TotalSources, r.QueriesToday, r.LastIndexed);
        }, default(CoreModels.KnowledgeStats?));

    public Task<CoreModels.KnowledgeLearnResponse?> KnowledgeLearnAsync(string content, string source, string? category = null, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.KnowledgeLearnAsync(new KnowledgeLearnRequestDto(content, source, category), ct).ConfigureAwait(false);
            return new CoreModels.KnowledgeLearnResponse(r.Success, r.EntryId, r.Error);
        }, default(CoreModels.KnowledgeLearnResponse?));

    // PIE
    public Task<CoreModels.PieInferResponse?> PieInferAsync(string premise, string? context = null, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.PieInferAsync(new PieInferRequestDto(premise, context), ct).ConfigureAwait(false);
            return new CoreModels.PieInferResponse(r.Conclusion, r.Confidence, r.SupportingEvidence);
        }, default(CoreModels.PieInferResponse?));

    public Task<CoreModels.PieChainResponse?> PieChainAsync(string initialPremise, int steps = 3, string? context = null, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.PieChainAsync(new PieChainRequestDto(initialPremise, steps, context), ct).ConfigureAwait(false);
            return new CoreModels.PieChainResponse(
                r.Steps?.Select(s => new CoreModels.PieChainStep(s.Step, s.Premise, s.Conclusion, s.Confidence)).ToList() ?? []);
        }, default(CoreModels.PieChainResponse?));

    public Task<CoreModels.PieKnowledgeResponse?> PieKnowledgeAsync(string domain, string fact, double certainty = 1.0, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.PieKnowledgeAsync(new PieKnowledgeRequestDto(domain, fact, certainty), ct).ConfigureAwait(false);
            return new CoreModels.PieKnowledgeResponse(r.Success);
        }, default(CoreModels.PieKnowledgeResponse?));

    public Task<CoreModels.PieCoherenceData?> PieCoherenceAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.PieCoherenceAsync(ct).ConfigureAwait(false);
            return new CoreModels.PieCoherenceData(r.OverallCoherence,
                r.Entries?.Select(e => new CoreModels.PieCoherenceEntry(e.Id, e.Statement, e.CoherenceScore)).ToList() ?? []);
        }, default(CoreModels.PieCoherenceData?));

    public Task<List<CoreModels.PieTerm>> PieTermsAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.PieTermsAsync(ct).ConfigureAwait(false);
            return r.Select(t => new CoreModels.PieTerm(t.Id, t.Name, t.Description, t.OccurrenceCount)).ToList();
        }, new List<CoreModels.PieTerm>());

    // Emotional history
    public Task<List<CoreModels.EmotionalHistoryEntry>> EmotionalHistoryAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.EmotionalHistoryAsync(ct).ConfigureAwait(false);
            return r.Select(e => new CoreModels.EmotionalHistoryEntry(e.Timestamp, e.Event, e.Valence, e.Arousal, e.Trigger)).ToList();
        }, new List<CoreModels.EmotionalHistoryEntry>());

    public Task<bool> EmotionalEventAsync(string @event, string? trigger = null, double? valenceDelta = null, double? arousalDelta = null, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.EmotionalEventAsync(new EmotionalEventRequestDto(@event, trigger, valenceDelta, arousalDelta), ct).ConfigureAwait(false);
            return r.Success;
        }, false);

    // Events
    public Task<List<CoreModels.EventInfo>> EventsRecentAsync(int take = 50, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.EventsRecentAsync(take, ct).ConfigureAwait(false);
            return r.Select(e => new CoreModels.EventInfo(e.EventId, e.Type, e.Description, e.Source, e.Timestamp, e.Metadata)).ToList();
        }, new List<CoreModels.EventInfo>());

    public Task<CoreModels.EventDetail?> EventDetailAsync(string eventId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.EventDetailAsync(eventId, ct).ConfigureAwait(false);
            return new CoreModels.EventDetail(r.EventId, r.Type, r.Description, r.Source, r.Timestamp, r.Metadata, r.RelatedEntityId, r.RelatedEntityType);
        }, default(CoreModels.EventDetail?));

    public Task<List<CoreModels.EventInfo>> EventsByMomentAsync(string momentId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.EventsByMomentAsync(momentId, ct).ConfigureAwait(false);
            return r.Select(e => new CoreModels.EventInfo(e.EventId, e.Type, e.Description, e.Source, e.Timestamp, e.Metadata)).ToList();
        }, new List<CoreModels.EventInfo>());

    public Task<List<Core.Models.ApprovalRequest>> GetPendingApprovalsAsync(string? role = null, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetPendingApprovalsAsync(role, ct).ConfigureAwait(false);
            return r.Select(MapApprovalRequest).ToList();
        }, []);

    public Task<Core.Models.ApprovalRequest?> GetApprovalDetailAsync(string requestId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetApprovalDetailAsync(requestId, ct).ConfigureAwait(false);
            return MapApprovalRequest(r);
        }, default(Core.Models.ApprovalRequest?));

    public Task<Core.Models.ApprovalRequest?> ApproveRequestAsync(string requestId, string? comment = null, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.ApproveRequestAsync(requestId, new ApprovalActionDto(comment), ct).ConfigureAwait(false);
            return MapApprovalRequest(r);
        }, default(Core.Models.ApprovalRequest?));

    public Task<Core.Models.ApprovalRequest?> RejectRequestAsync(string requestId, string? comment = null, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.RejectRequestAsync(requestId, new ApprovalActionDto(comment), ct).ConfigureAwait(false);
            return MapApprovalRequest(r);
        }, default(Core.Models.ApprovalRequest?));

    // Coding
    public Task<Core.Models.CodingResponse?> CodingExplainAsync(Core.Models.CodingRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new Core.Models.CodingResponse(null, "Not implemented", false, null), default(Core.Models.CodingResponse?));
    public Task<Core.Models.CodingResponse?> CodingFixAsync(Core.Models.CodingRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new Core.Models.CodingResponse(null, "Not implemented", false, null), default(Core.Models.CodingResponse?));
    public Task<Core.Models.CodingResponse?> CodingGenerateTestsAsync(Core.Models.CodingRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new Core.Models.CodingResponse(null, "Not implemented", false, null), default(Core.Models.CodingResponse?));
    public Task<Core.Models.CodingResponse?> CodingReviewAsync(Core.Models.CodingRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new Core.Models.CodingResponse(null, "Not implemented", false, null), default(Core.Models.CodingResponse?));
    public Task<Core.Models.CodingResponse?> CodingApplyDiffAsync(Core.Models.CodingRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new Core.Models.CodingResponse(null, "Not implemented", false, null), default(Core.Models.CodingResponse?));
    public Task<Core.Models.CodingResponse?> CodingCompleteAsync(Core.Models.CodingRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new Core.Models.CodingResponse(null, "Not implemented", false, null), default(Core.Models.CodingResponse?));
    public Task<Core.Models.CodingStatus?> GetCodingStatusAsync(string cycleId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new Core.Models.CodingStatus(cycleId, "unknown", null, 0, null, null, DateTime.UtcNow, null), default(Core.Models.CodingStatus?));

    // Self-Improvement
    public Task<Core.Models.SelfImprovementStatus?> GetSelfImprovementStatusAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new Core.Models.SelfImprovementStatus(false, false, DateTime.UtcNow, 0, 0, 0, [], []), default(Core.Models.SelfImprovementStatus?));

    // Assistant (Threads)
    public Task<Core.Models.ThreadInfo?> CreateThreadAsync(string? title = null, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new Core.Models.ThreadInfo(Guid.NewGuid().ToString("N"), title ?? "New Thread", DateTime.UtcNow, "active"), default(Core.Models.ThreadInfo?));
    public Task<Core.Models.ThreadInfo?> GetThreadAsync(string threadId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new Core.Models.ThreadInfo(threadId, "Not implemented", DateTime.UtcNow, "active"), default(Core.Models.ThreadInfo?));
    public Task<Core.Models.MessageInfo?> SendMessageAsync(string threadId, string content, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new Core.Models.MessageInfo(Guid.NewGuid().ToString("N"), threadId, "user", content, DateTime.UtcNow, null), default(Core.Models.MessageInfo?));
    public Task<List<Core.Models.MessageInfo>> GetMessagesAsync(string threadId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new List<Core.Models.MessageInfo>(), new List<Core.Models.MessageInfo>());
    public Task<Core.Models.RunInfo?> CreateRunAsync(string threadId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new Core.Models.RunInfo(Guid.NewGuid().ToString("N"), threadId, "completed", null, DateTime.UtcNow, null, null), default(Core.Models.RunInfo?));
    public Task<Core.Models.RunInfo?> GetRunAsync(string threadId, string runId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new Core.Models.RunInfo(runId, threadId, "completed", null, DateTime.UtcNow, null, null), default(Core.Models.RunInfo?));
    public Task<bool> CancelRunAsync(string threadId, string runId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => true, false);

    // MCP Config
    public Task<Core.Models.McpServerConfig?> GetMcpServerConfigAsync(string serverId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new Core.Models.McpServerConfig(serverId, serverId, "stdio", "", null, null), default(Core.Models.McpServerConfig?));
    public Task<bool> UpdateMcpServerAsync(string serverId, Core.Models.McpServerConfig config, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => true, false);

    // Plan
    public Task<CoreModels.PlanExecutionResult?> GetCurrentPlanAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetCognitiveDashboardAsync(ct).ConfigureAwait(false);
            if (r?.ActiveModules == null || r.ActiveModules.Count == 0)
                return default(CoreModels.PlanExecutionResult?);
            var steps = new List<CoreModels.PlanStep>
            {
                new(0, "Inicialização", "Módulos cognitivos iniciados", CoreModels.PlanStepStatus.Completed, DateTime.UtcNow.AddMinutes(-30), DateTime.UtcNow.AddMinutes(-28), "Ok"),
                new(1, "Processamento", "Processamento ativo", CoreModels.PlanStepStatus.InProgress, DateTime.UtcNow.AddMinutes(-28), null, null),
                new(2, "Consolidação", "Consolidando aprendizado", CoreModels.PlanStepStatus.Pending, null, null, null),
            };
            var plan = new CoreModels.PlanInfo("plan-1", r.OverallHealth >= 70 ? "Operação normal" : "Recuperação",
                "Plano de execução do sistema cognitivo", CoreModels.PlanStatus.InProgress,
                r.ActiveModules.Count > 0 ? (double)r.ActiveModules.Count(m => m.Status == "healthy") / r.ActiveModules.Count : 0,
                3, 1, DateTime.UtcNow.AddHours(-1), null);
            return new CoreModels.PlanExecutionResult("plan-1", true, plan, steps, null);
        }, default(CoreModels.PlanExecutionResult?));

    public Task<List<CoreModels.PlanStep>> GetPlanStepsAsync(string planId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new List<CoreModels.PlanStep>(), []);

    // Feedback History
    public Task<List<CoreModels.FeedbackHistoryEntry>> GetFeedbackHistoryAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new List<CoreModels.FeedbackHistoryEntry>
        {
            new("fb-demo-1", "ep-demo-1", 5, "Ótimo sistema!", "geral", DateTimeOffset.UtcNow.AddDays(-1)),
            new("fb-demo-2", "ep-demo-2", 4, "Bom desempenho", "performance", DateTimeOffset.UtcNow.AddDays(-2)),
            new("fb-demo-3", "ep-demo-3", 3, "Pode melhorar", "usabilidade", DateTimeOffset.UtcNow.AddDays(-3)),
        }, []);

    public Task<CoreModels.FeedbackAverage?> GetFeedbackAverageAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new CoreModels.FeedbackAverage(3, 4.0, 0, 0, 1, 1, 1), default(CoreModels.FeedbackAverage?));

    // Episodic Memory
    public Task<CoreModels.EpisodicMemorySearchResult?> SearchEpisodicMemoryAsync(CoreModels.EpisodicMemorySearchRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.SearchEpisodesAsync(request.Query, null, request.Status,
                request.FromDate, request.ToDate, 1, request.TopK, ct).ConfigureAwait(false);
            return new CoreModels.EpisodicMemorySearchResult(
                r.Episodes?.Select(e => new CoreModels.EpisodicMemoryHit(e.Id, e.GoalId,
                    e.Outcome ?? "Sem resumo", e.Status, e.SuccessRate, e.CreatedAt)).ToList() ?? [],
                r.TotalCount, request.Query);
        }, default(CoreModels.EpisodicMemorySearchResult?));

    // User Services
    public Task<List<Core.Models.UserServiceInfo>> GetUserServicesAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new List<Core.Models.UserServiceInfo>(), new List<Core.Models.UserServiceInfo>());
    public Task<bool> UpdateUserServiceAsync(string serviceType, Core.Models.UserServiceUpdateRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => true, false);
    public Task<bool> DeleteUserServiceAsync(string serviceType, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => true, false);

    // Cognitive Flow (Studio) — stub until backend endpoints exist
    public Task<Core.Models.CognitiveFlowResult?> CognitiveFlowExecuteAsync(Core.Models.FlowDefinition flow, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => new Core.Models.CognitiveFlowResult(false, null, "Not implemented via API"), default(Core.Models.CognitiveFlowResult?));
    public Task<bool> CognitiveFlowSaveAsync(Core.Models.FlowDefinition flow, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => false, false);
    public Task<Core.Models.FlowDefinition?> CognitiveFlowLoadAsync(string flowName, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => default(Core.Models.FlowDefinition?), default(Core.Models.FlowDefinition?));

    // Templates
    public Task<List<CoreModels.TemplateInfo>> TemplateListAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetTemplatesAsync(ct).ConfigureAwait(false);
            return r.Select(t => new CoreModels.TemplateInfo(t.Id, t.Name, t.Description, t.Content, t.Category, t.Version, t.CreatedAt, t.UpdatedAt)).ToList();
        }, new List<CoreModels.TemplateInfo>());

    public Task<CoreModels.TemplateInfo?> TemplateGetAsync(string templateId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetTemplateAsync(templateId, ct).ConfigureAwait(false);
            return new CoreModels.TemplateInfo(r.Id, r.Name, r.Description, r.Content, r.Category, r.Version, r.CreatedAt, r.UpdatedAt);
        }, default(CoreModels.TemplateInfo?));

    public Task<CoreModels.TemplateInfo?> TemplateCreateAsync(CoreModels.CreateTemplateRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.CreateTemplateAsync(request, ct).ConfigureAwait(false);
            return new CoreModels.TemplateInfo(r.Id, r.Name, r.Description, r.Content, r.Category, r.Version, r.CreatedAt, r.UpdatedAt);
        }, default(CoreModels.TemplateInfo?));

    public Task<CoreModels.TemplateInfo?> TemplateUpdateAsync(string templateId, CoreModels.UpdateTemplateRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.UpdateTemplateAsync(templateId, request, ct).ConfigureAwait(false);
            return new CoreModels.TemplateInfo(r.Id, r.Name, r.Description, r.Content, r.Category, r.Version, r.CreatedAt, r.UpdatedAt);
        }, default(CoreModels.TemplateInfo?));

    public Task<bool> TemplateDeleteAsync(string templateId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => { await api.DeleteTemplateAsync(templateId, ct).ConfigureAwait(false); return true; }, false);

    public Task<CoreModels.TemplateRenderResult?> TemplateRenderAsync(string templateId, CoreModels.RenderTemplateRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.RenderTemplateAsync(templateId, request, ct).ConfigureAwait(false);
            return new CoreModels.TemplateRenderResult(r.RenderedContent, r.Error);
        }, default(CoreModels.TemplateRenderResult?));

    // Experiments
    public Task<List<CoreModels.ExperimentInfo>> ExperimentListAsync(CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetExperimentsAsync(ct).ConfigureAwait(false);
            return r.Select(e => new CoreModels.ExperimentInfo(e.Id, e.Name, e.Status, e.Description, e.CreatedAt, e.CompletedAt)).ToList();
        }, new List<CoreModels.ExperimentInfo>());

    public Task<CoreModels.ExperimentInfo?> ExperimentStartAsync(CoreModels.StartExperimentRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.StartExperimentAsync(request, ct).ConfigureAwait(false);
            return new CoreModels.ExperimentInfo(r.Id, r.Name, r.Status, r.Description, r.CreatedAt, r.CompletedAt);
        }, default(CoreModels.ExperimentInfo?));

    public Task<bool> ExperimentCompleteAsync(string experimentId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => { await api.CompleteExperimentAsync(experimentId, ct).ConfigureAwait(false); return true; }, false);

    public Task<bool> ExperimentRecordMetricAsync(string experimentId, CoreModels.RecordMetricRequest request, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () => { await api.RecordMetricAsync(experimentId, request, ct).ConfigureAwait(false); return true; }, false);

    public Task<CoreModels.ExperimentAnalysis?> ExperimentGetAnalysisAsync(string experimentId, CancellationToken ct = default) =>
        SafeCall.ExecuteAsync(async () =>
        {
            var r = await api.GetExperimentAnalysisAsync(experimentId, ct).ConfigureAwait(false);
            return new CoreModels.ExperimentAnalysis(r.ExperimentId, r.TotalMetrics, r.AvgValue, r.AvgLatencyMs, r.SuccessRate,
                r.Metrics.Select(m => new CoreModels.MetricEntry(m.Name, m.Value, m.Timestamp)).ToList(), r.Insights);
        }, default(CoreModels.ExperimentAnalysis?));

    private static Core.Models.ApprovalRequest MapApprovalRequest(Abstractions.ApprovalRequestDto dto)
    {
        var status = dto.Status switch
        {
            "Pending" => CoreModels.ApprovalStatus.Pending,
            "Approved" => CoreModels.ApprovalStatus.Approved,
            "Rejected" => CoreModels.ApprovalStatus.Rejected,
            "Expired" => CoreModels.ApprovalStatus.Expired,
            "Escalated" => CoreModels.ApprovalStatus.Escalated,
            _ => CoreModels.ApprovalStatus.Pending
        };
        return new Core.Models.ApprovalRequest(
            dto.RequestId, dto.ActionId, dto.ActionType, dto.Description,
            dto.PayloadJson, dto.RiskScore, dto.RequiredApprovers ?? [],
            dto.CreatedAt, dto.Deadline, status,
            dto.Responses?.Select(r => new Core.Models.ApprovalResponse(
                r.ApproverId, r.ApproverName, r.Approved, r.Comment, r.Timestamp
            )).ToList() ?? [],
            dto.AgentName, dto.RequestedBy);
    }
}
