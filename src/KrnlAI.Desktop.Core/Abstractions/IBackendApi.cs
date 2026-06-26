using KrnlAI.Contracts;

namespace KrnlAI.Desktop.Core.Abstractions;

/// <summary>Interface enxuta com os métodos essenciais do backend KrnlAI.
/// Used by both WPF (via IKernelClient) and sidecar/other consumers.</summary>
public interface IBackendApi
{
    // ── Health ────────────────────────────────────────────────
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);

    // ── Agent ─────────────────────────────────────────────────
    Task<AgentRunTransportResponse> RunAgentAsync(AgentRunTransportRequest request, CancellationToken cancellationToken = default);

    // ── Memory ────────────────────────────────────────────────
    Task<Models.MemorySearchResult> SearchMemoryAsync(string query, int topK = 10, CancellationToken cancellationToken = default);
    Task<Models.MemoryIngestResult> IngestMemoryAsync(Models.MemoryIngestRequest request, CancellationToken cancellationToken = default);
    Task<Models.MemoryMetrics?> GetMemoryMetricsAsync(CancellationToken cancellationToken = default);
    Task<Models.WorkingMemorySummary?> GetWorkingMemoryAsync(CancellationToken cancellationToken = default);

    // ── Policies ──────────────────────────────────────────────
    Task<Models.PolicyListResponse> GetPoliciesAsync(string? domain = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<Models.PolicyDetails?> GetPolicyAsync(string policyId, CancellationToken cancellationToken = default);
    Task<Models.PolicyInfo?> CreatePolicyAsync(Models.CreatePolicyRequest request, CancellationToken cancellationToken = default);
    Task<Models.PolicyInfo?> UpdatePolicyAsync(string policyId, Models.UpdatePolicyRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeletePolicyAsync(string policyId, CancellationToken cancellationToken = default);

    // ── Episodes ──────────────────────────────────────────────
    Task<Models.EpisodeSearchResult> SearchEpisodesAsync(Models.EpisodeSearchRequest request, CancellationToken cancellationToken = default);
    Task<Models.EpisodeDetails?> GetEpisodeAsync(string episodeId, CancellationToken cancellationToken = default);

    // ── Dashboard / Metrics ───────────────────────────────────
    Task<Models.AgentMetricsSummary?> GetMetricsSummaryAsync(CancellationToken cancellationToken = default);
    Task<Models.AgentScorecard?> GetScorecardAsync(CancellationToken cancellationToken = default);
    Task<Models.RuntimeSummary?> GetRuntimeSummaryAsync(CancellationToken cancellationToken = default);
    Task<Models.CognitiveDashboardData?> GetCognitiveDashboardAsync(CancellationToken cancellationToken = default);

    // ── Goals ─────────────────────────────────────────────────
    Task<Models.GoalListResponse> GetActiveGoalsAsync(CancellationToken cancellationToken = default);
    Task<Models.GoalDetails?> GetGoalAsync(string goalId, CancellationToken cancellationToken = default);
    Task<Models.GoalInfo?> CreateGoalAsync(Models.CreateGoalRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateGoalStatusAsync(string goalId, string action, CancellationToken cancellationToken = default);

    // ── Cognitive / Causal ────────────────────────────────────
    Task<Models.BenchmarkSummary?> GetBenchmarkSummaryAsync(CancellationToken cancellationToken = default);
    Task<Models.CausalQueryResult?> GetCausalQueryAsync(string query, CancellationToken cancellationToken = default);
    Task<Models.CausalPrediction?> GetCausalPredictionAsync(string action, CancellationToken cancellationToken = default);

    // ── Feedback ──────────────────────────────────────────────
    Task<Models.FeedbackResponse> SubmitFeedbackAsync(Models.FeedbackRequest request, CancellationToken cancellationToken = default);

    // ── Versions ──────────────────────────────────────────────
    Task<Models.VersionsInfo?> GetVersionsAsync(CancellationToken cancellationToken = default);

    // ── Emotional ─────────────────────────────────────────────
    Task<Models.EmotionalState?> GetEmotionalStateAsync(string userId, CancellationToken cancellationToken = default);
    Task<Models.AffectiveState?> GetAffectiveStateAsync(CancellationToken cancellationToken = default);

    // ── Cross-Service ─────────────────────────────────────────
    Task<Models.CrossSummaryData?> GetCrossSummaryAsync(CancellationToken cancellationToken = default);
    Task<Models.MetricsByGoalData?> GetMetricsByGoalAsync(CancellationToken cancellationToken = default);
}
