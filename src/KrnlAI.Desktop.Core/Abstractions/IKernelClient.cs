using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

/// <summary>Client for communicating with the Krnl-AI backend API.</summary>
public interface IKernelClient : IKernelAgentClient, IKernelSpeechClient
{
    void SetAuthToken(string? token);
    void SetBaseUrl(string baseUrl);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Core.Models.PolicyListResponse> GetPoliciesAsync(string? domain = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<Core.Models.PolicyDetails?> GetPolicyAsync(string policyId, CancellationToken cancellationToken = default);
    Task<Core.Models.PolicyInfo?> CreatePolicyAsync(Core.Models.CreatePolicyRequest request, CancellationToken cancellationToken = default);
    Task<Core.Models.PolicyInfo?> UpdatePolicyAsync(string policyId, Core.Models.UpdatePolicyRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeletePolicyAsync(string policyId, CancellationToken cancellationToken = default);
    Task<Core.Models.EpisodeSearchResult> SearchEpisodesAsync(Core.Models.EpisodeSearchRequest request, CancellationToken cancellationToken = default);
    Task<Core.Models.EpisodeDetails?> GetEpisodeAsync(string episodeId, CancellationToken cancellationToken = default);
    Task<Core.Models.MemorySearchResult> SearchMemoryAsync(string query, int topK = 10, CancellationToken cancellationToken = default);
    Task<Core.Models.MemoryIngestResult> IngestMemoryAsync(Core.Models.MemoryIngestRequest request, CancellationToken cancellationToken = default);
    Task<Core.Models.MemoryMetrics?> GetMemoryMetricsAsync(CancellationToken cancellationToken = default);
    Task<Core.Models.WorkingMemorySummary?> GetWorkingMemoryAsync(CancellationToken cancellationToken = default);
    Task<Core.Models.AgentMetricsSummary?> GetMetricsSummaryAsync(CancellationToken cancellationToken = default);
    Task<Core.Models.AgentScorecard?> GetScorecardAsync(CancellationToken cancellationToken = default);
    Task<Core.Models.RuntimeSummary?> GetRuntimeSummaryAsync(CancellationToken cancellationToken = default);
    Task<Core.Models.GoalListResponse> GetActiveGoalsAsync(CancellationToken cancellationToken = default);
    Task<Core.Models.GoalDetails?> GetGoalAsync(string goalId, CancellationToken cancellationToken = default);
    Task<Core.Models.GoalInfo?> CreateGoalAsync(Core.Models.CreateGoalRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateGoalStatusAsync(string goalId, string action, CancellationToken cancellationToken = default);
    Task<Core.Models.FeedbackResponse> SubmitFeedbackAsync(Core.Models.FeedbackRequest request, CancellationToken cancellationToken = default);
    Task<Core.Models.CognitiveDashboardData?> GetCognitiveDashboardAsync(CancellationToken cancellationToken = default);
    Task<Core.Models.UserProfile?> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateUserProfileAsync(Core.Models.UserProfile profile, CancellationToken cancellationToken = default);
    Task<Core.Models.MultimodalSearchResult?> SearchMultimodalAsync(string query, int topK = 10, CancellationToken cancellationToken = default);
    Task<Core.Models.BenchmarkSummary?> GetBenchmarkSummaryAsync(CancellationToken cancellationToken = default);
    Task<Core.Models.CausalQueryResult?> GetCausalQueryAsync(string query, CancellationToken cancellationToken = default);
    Task<Core.Models.CausalPrediction?> GetCausalPredictionAsync(string action, CancellationToken cancellationToken = default);
    Task<Core.Models.CrossSummaryData?> GetCrossSummaryAsync(CancellationToken cancellationToken = default);
    Task<Core.Models.MetricsByGoalData?> GetMetricsByGoalAsync(CancellationToken cancellationToken = default);
    Task<Core.Models.PolicyVersionList?> GetPolicyVersionsAsync(string policyId, CancellationToken cancellationToken = default);
    Task<List<Core.Models.PolicyRollbackEntry>> GetPolicyRollbacksAsync(string policyId, CancellationToken cancellationToken = default);
    Task<Core.Models.GoalCycleList?> GetGoalCyclesAsync(string goalId, CancellationToken cancellationToken = default);
    Task<Core.Models.EmotionalState?> GetEmotionalStateAsync(string userId, CancellationToken cancellationToken = default);
    Task<Core.Models.AffectiveState?> GetAffectiveStateAsync(CancellationToken cancellationToken = default);
    Task<List<Core.Models.McpServerInfo>> GetMcpServersAsync(CancellationToken cancellationToken = default);
    Task<bool> ToggleMcpServerAsync(string serverId, bool enabled, CancellationToken cancellationToken = default);
    Task<List<Core.Models.DocumentInfo>> GetDocumentsAsync(int limit = 50, CancellationToken cancellationToken = default);
    Task<Core.Models.DocumentInfo?> GetDocumentStatusAsync(string documentId, CancellationToken cancellationToken = default);
    Task<Core.Models.ArchiveStats?> GetArchiveStatsAsync(CancellationToken cancellationToken = default);
    Task<Core.Models.VersionsInfo?> GetVersionsAsync(CancellationToken cancellationToken = default);
    Task<Core.Models.ContractsResponse?> GetContractsAsync(CancellationToken cancellationToken = default);
    Task<Core.Models.ModelRegistryDetail?> GetModelRegistryAsync(string modelId, CancellationToken cancellationToken = default);
    Task<Core.Models.ShareListResponse?> GetSharesAsync(CancellationToken cancellationToken = default);
}
