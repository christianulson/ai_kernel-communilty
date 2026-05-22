using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

/// <summary>Client for communicating with the Krnl-AI backend API.</summary>
public interface IKernelClient : IAuthClient, IMemoryClient, IPolicyClient, IEpisodeClient, IDashboardClient, IGoalClient, IAdminClient, IKernelAgentClient, IKernelSpeechClient, ISnapshotClient, IObjectiveClient, IInvestigationClient
{
    void SetBaseUrl(string baseUrl);
    Task<Core.Models.FeedbackResponse> SubmitFeedbackAsync(Core.Models.FeedbackRequest request, CancellationToken cancellationToken = default);
    Task<Core.Models.CognitiveDashboardData?> GetCognitiveDashboardAsync(CancellationToken cancellationToken = default);
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
}
