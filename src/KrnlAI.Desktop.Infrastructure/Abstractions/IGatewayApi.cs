using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Refit;

namespace KrnlAI.Desktop.Infrastructure.Abstractions;

public interface IGatewayApi
{
    [Post("/agent/run")]
    Task<AgentRunResponseDto> RunAgentAsync([Body] AgentRunRequest request, CancellationToken ct);

    [Get("/health")]
    Task<HealthResponse> GetHealthAsync(CancellationToken ct);

    [Get("/policy/list")]
    Task<PolicyListResponseDto> GetPoliciesAsync(int page, int pageSize, string? domain = null, CancellationToken ct = default);

    [Get("/policy/{id}")]
    Task<PolicyDetailsDto> GetPolicyAsync(string id, CancellationToken ct);

    [Post("/policy")]
    Task<PolicyInfoDto> CreatePolicyAsync([Body] Core.Models.CreatePolicyRequest request, CancellationToken ct);

    [Put("/policy/{id}")]
    Task<PolicyInfoDto> UpdatePolicyAsync(string id, [Body] Core.Models.UpdatePolicyRequest request, CancellationToken ct);

    [Delete("/policy/{id}")]
    Task DeletePolicyAsync(string id, CancellationToken ct);

    [Get("/episodes/search")]
    Task<EpisodeSearchResultDto> SearchEpisodesAsync(string? q, string? goalId, string? status, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken ct);

    [Get("/episodes/{id}")]
    Task<EpisodeDetailsDto> GetEpisodeAsync(string id, CancellationToken ct);

    [Post("/memory/search")]
    Task<MemorySearchResultDto> SearchMemoryAsync([Body] MemorySearchRequestDto request, CancellationToken ct);

    [Post("/memory/ingest")]
    Task<MemoryIngestResultDto> IngestMemoryAsync([Body] Core.Models.MemoryIngestRequest request, CancellationToken ct);

    [Get("/memory/metrics")]
    Task<MemoryMetricsDto> GetMemoryMetricsAsync(CancellationToken ct);

    [Get("/memory/working")]
    Task<WorkingMemoryDto> GetWorkingMemoryAsync(CancellationToken ct);

    [Get("/agent/metrics/summary")]
    Task<AgentMetricsDto> GetMetricsSummaryAsync(CancellationToken ct);

    [Get("/agent/metrics/scorecard")]
    Task<ScorecardDto> GetScorecardAsync(CancellationToken ct);

    [Get("/agent/metrics/summary/by-goal")]
    Task<MetricsByGoalDto> GetMetricsByGoalAsync(CancellationToken ct);

    [Get("/agent/metrics/cycles")]
    Task<List<GoalCycleSummaryDto>> GetGoalCyclesAsync(string goalId, CancellationToken ct);

    [Get("/observability/runtime/summary")]
    Task<RuntimeSummaryDto> GetRuntimeSummaryAsync(CancellationToken ct);

    [Get("/observability/runtime/cross-summary")]
    Task<CrossSummaryDto> GetCrossSummaryAsync(CancellationToken ct);

    [Get("/goals/active")]
    Task<GoalListDto> GetActiveGoalsAsync(CancellationToken ct);

    [Get("/goals/{id}")]
    Task<GoalDetailsDto> GetGoalAsync(string id, CancellationToken ct);

    [Post("/goals")]
    Task<GoalInfoDto> CreateGoalAsync([Body] Core.Models.CreateGoalRequest request, CancellationToken ct);

    [Post("/goals/{id}/{action}")]
    Task UpdateGoalStatusAsync(string id, string action, CancellationToken ct);

    [Post("/agent/feedback")]
    Task<FeedbackResultDto> SubmitFeedbackAsync([Body] Core.Models.FeedbackRequest request, CancellationToken ct);

    [Get("/cognitive/dashboard")]
    Task<CognitiveDashboardDto> GetCognitiveDashboardAsync(CancellationToken ct);

    [Get("/profile/{userId}")]
    Task<UserProfileDto> GetUserProfileAsync(string userId, CancellationToken ct);

    [Post("/profile")]
    Task UpdateUserProfileAsync([Body] Core.Models.UserProfile request, CancellationToken ct);

    [Get("/benchmark/summary")]
    Task<BenchmarkDto> GetBenchmarkSummaryAsync(CancellationToken ct);

    [Get("/causal/causes")]
    Task<CausalQueryDto> GetCausalCausesAsync(string query, CancellationToken ct);

    [Get("/causal/predict")]
    Task<CausalPredictionDto> GetCausalPredictAsync(string action, CancellationToken ct);

    [Get("/policy/versions")]
    Task<PolicyVersionListDto> GetPolicyVersionsAsync(string policyId, CancellationToken ct);

    [Get("/policy/rollbacks")]
    Task<List<PolicyRollbackDto>> GetPolicyRollbacksAsync(string policyId, CancellationToken ct);

    [Post("/auth/login")]
    Task<LoginResponseDto> LoginAsync([Body] Core.Models.LoginRequest request, CancellationToken ct);

    [Post("/auth/refresh")]
    Task<RefreshTokenResponseDto> RefreshTokenAsync([Body] RefreshTokenRequest request, CancellationToken ct = default);

    [Post("/auth/oauth2/callback")]
    Task<LoginResponseDto> OAuth2CallbackAsync([Body] OAuth2CallbackRequest request, CancellationToken ct = default);

    [Post("/auth/oauth2/login")]
    Task<OAuth2LoginResponse> OAuth2LoginAsync([Body] OAuth2LoginRequest request, CancellationToken ct = default);

    [Post("/media/tts")]
    Task<TtsResponseDto> GenerateSpeechAsync([Body] Core.Models.SpeechRequest request, CancellationToken ct);

    [Post("/audio/transcribe")]
    Task<TranscriptionResponseDto> TranscribeAudioAsync([Body] Core.Models.TranscribeRequest request, CancellationToken ct);

    [Post("/memory/multimodal/search")]
    Task<MultimodalSearchResultDto> SearchMultimodalAsync([Body] Core.Models.MultimodalSearchRequest request, CancellationToken ct);

    [Get("/profile/emotional")]
    Task<EmotionalStateDto> GetEmotionalStateAsync(string userId, CancellationToken ct);

    [Get("/cognitive/affective-state")]
    Task<AffectiveStateDto> GetAffectiveStateAsync(CancellationToken ct);

    // MCP Configuration
    [Get("/user/mcp/servers")]
    Task<List<McpServerStatusDto>> GetMcpServersAsync(CancellationToken ct);

    [Post("/user/mcp/servers/{serverId}/toggle")]
    Task ToggleMcpServerAsync(string serverId, [Body] McpToggleRequest request, CancellationToken ct);

    [Get("/api/documents?limit={limit}")]
    Task<List<DocumentInfoDto>> GetDocumentsAsync(int limit, CancellationToken ct);

    [Get("/api/documents/{id}/status")]
    Task<DocumentInfoDto> GetDocumentStatusAsync(string id, CancellationToken ct);

    [Get("/archive/stats")]
    Task<ArchiveStatsDto> GetArchiveStatsAsync(CancellationToken ct);

    [Get("/versions")]
    Task<VersionsInfoDto> GetVersionsAsync(CancellationToken ct);

    [Get("/versions/contracts")]
    Task<ContractsResponseDto> GetContractsAsync(CancellationToken ct);

    [Get("/observability/neural/model-registry/{modelId}")]
    Task<ModelRegistryDetailDto> GetModelRegistryAsync(string modelId, CancellationToken ct);

    [Get("/sessions/shares")]
    Task<ShareListResponseDto> GetSharesAsync(CancellationToken ct);

    [Get("/snapshots")]
    Task<List<SnapshotResponseDto>> GetSnapshotsAsync(CancellationToken ct = default);

    [Get("/objectives/active")]
    Task<List<ObjectiveResponseDto>> GetObjectivesAsync(CancellationToken ct = default);

    [Get("/objectives/{id}")]
    Task<ObjectiveDetailResponseDto> GetObjectiveDetailAsync(string id, CancellationToken ct = default);

    [Get("/investigations")]
    Task<List<InvestigationResponseDto>> GetInvestigationsAsync(CancellationToken ct = default);
}

public sealed record McpServerStatusDto(string ServerId, string Name, string TransportType, bool Enabled, bool IsConnected, int ToolCount, DateTimeOffset? LastUsedAt);
public sealed record McpToggleRequest(bool Enabled);

public sealed record DocumentInfoDto(string DocumentId, string FileName, long FileSize, string Format, string Status, string? ErrorMessage, int ChunkCount, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt);

public sealed record ArchiveStatsDto(bool Ok, int TotalArchived, List<string>? Stores);
public sealed record VersionsInfoDto(string DefaultVersion, List<string>? SupportedVersions, bool LegacyUnversionedDeprecated, string LegacySunsetDate);
public sealed record ContractsResponseDto(string DefaultApiVersion, List<ContractEntryDto>? Contracts);
public sealed record ContractEntryDto(string Endpoint, string ContractVersion, string SupportedRange, bool Deprecated, string State);
public sealed record ModelRegistryDetailDto(string ModelId, List<ModelRegistryEntryDto>? Models, ModelRegistryEntryDto? Active);
public sealed record ModelRegistryEntryDto(string ModelId, string ModelVersion, string UseCase, string Runtime, string Status, string? ApprovedBy, DateTimeOffset CreatedAt, DateTimeOffset? ActivatedAt);
public sealed record ShareListResponseDto(List<ShareDto>? Shares);
public sealed record ShareDto(string ShareCode, string SessionId, string AccessLevel, DateTime CreatedAt, DateTime? ExpiresAt, int AccessCount, bool IsRevoked);
public sealed record SnapshotResponseDto(string SnapshotId, string Label, DateTime CreatedAt, long Size);
public sealed record ObjectiveResponseDto(string ObjectiveId, string Description, string Status, double Progress, int Priority, string? Deadline);
public sealed record ObjectiveDetailResponseDto(string ObjectiveId, string Description, string Status, double Progress, List<TargetResponseDto> Targets);
public sealed record TargetResponseDto(string TargetId, string Description, double CurrentValue, double TargetValue, string Unit);
public sealed record InvestigationResponseDto(string CaseId, string Title, string Status, int EvidenceCount, DateTime CreatedAt);

// Approvals
[Get("/approvals/pending")]
Task<List<ApprovalRequestDto>> GetPendingApprovalsAsync(string? role = null, CancellationToken ct = default);

[Get("/approvals/{requestId}")]
Task<ApprovalRequestDto> GetApprovalDetailAsync(string requestId, CancellationToken ct);

[Post("/approvals/{requestId}/approve")]
Task<ApprovalRequestDto> ApproveRequestAsync(string requestId, [Body] ApprovalActionDto body, CancellationToken ct);

[Post("/approvals/{requestId}/reject")]
Task<ApprovalRequestDto> RejectRequestAsync(string requestId, [Body] ApprovalActionDto body, CancellationToken ct);

public sealed record ApprovalActionDto(string? Comment);
public sealed record ApprovalRequestDto(
    string RequestId, string ActionId, string ActionType, string Description,
    string? PayloadJson, double RiskScore, string[] RequiredApprovers,
    DateTimeOffset CreatedAt, DateTimeOffset Deadline, string Status,
    List<ApprovalResponseDto>? Responses, string? AgentName, string? RequestedBy);
public sealed record ApprovalResponseDto(
    string ApproverId, string ApproverName, bool Approved, string? Comment, DateTimeOffset Timestamp);
