using KrnlAI.Contracts;
using KrnlAI.Desktop.Core.Abstractions;
using Refit;

namespace KrnlAI.Desktop.Infrastructure.Abstractions;

public interface IGatewayApi
{
    [Post("/agent/run")]
    Task<AgentRunResponseDto> RunAgentAsync([Body] AgentRunTransportRequest request, CancellationToken ct);

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

    // Approvals
    [Get("/approvals/pending")]
    Task<List<ApprovalRequestDto>> GetPendingApprovalsAsync(string? role = null, CancellationToken ct = default);
    [Get("/approvals/{requestId}")]
    Task<ApprovalRequestDto> GetApprovalDetailAsync(string requestId, CancellationToken ct);
    [Post("/approvals/{requestId}/approve")]
    Task<ApprovalRequestDto> ApproveRequestAsync(string requestId, [Body] ApprovalActionDto body, CancellationToken ct);
    [Post("/approvals/{requestId}/reject")]
    Task<ApprovalRequestDto> RejectRequestAsync(string requestId, [Body] ApprovalActionDto body, CancellationToken ct);

    // Knowledge
    [Get("/api/knowledge/ask")]
    Task<KnowledgeQueryResultDto> KnowledgeAskAsync(string q, CancellationToken ct);
    [Get("/api/knowledge/stats")]
    Task<KnowledgeStatsDto> KnowledgeStatsAsync(CancellationToken ct);
    [Post("/api/knowledge/learn")]
    Task<KnowledgeLearnResultDto> KnowledgeLearnAsync([Body] KnowledgeLearnRequestDto request, CancellationToken ct);

    // PIE
    [Post("/api/cognitive/pie/infer")]
    Task<PieInferResultDto> PieInferAsync([Body] PieInferRequestDto request, CancellationToken ct);
    [Post("/api/cognitive/pie/chain")]
    Task<PieChainResultDto> PieChainAsync([Body] PieChainRequestDto request, CancellationToken ct);
    [Post("/api/cognitive/pie/knowledge")]
    Task<PieKnowledgeResultDto> PieKnowledgeAsync([Body] PieKnowledgeRequestDto request, CancellationToken ct);
    [Get("/api/cognitive/pie/coherence")]
    Task<PieCoherenceResultDto> PieCoherenceAsync(CancellationToken ct);
    [Get("/api/cognitive/pie/terms")]
    Task<List<PieTermDto>> PieTermsAsync(CancellationToken ct);

    // Emotional history
    [Post("/profile/emotional/event")]
    Task<EmotionalEventResultDto> EmotionalEventAsync([Body] EmotionalEventRequestDto request, CancellationToken ct);
    [Get("/profile/emotional/history")]
    Task<List<EmotionalHistoryEntryDto>> EmotionalHistoryAsync(CancellationToken ct);

    // Episodic Memory
    [Post("/episode-memory/search")]
    Task<EpisodicMemorySearchResultDto> SearchEpisodicMemoryAsync([Body] EpisodicMemorySearchRequestDto request, CancellationToken ct);

    // Events
    [Get("/events/recent")]
    Task<List<EventInfoDto>> EventsRecentAsync(int take, CancellationToken ct);
    [Get("/events/{eventId}")]
    Task<EventDetailDto> EventDetailAsync(string eventId, CancellationToken ct);
    [Get("/events/by-moment/{momentId}")]
    Task<List<EventInfoDto>> EventsByMomentAsync(string momentId, CancellationToken ct);

    // Templates
    [Get("/api/templates")]
    Task<List<TemplateInfoDto>> GetTemplatesAsync(CancellationToken ct);
    [Get("/api/templates/{id}")]
    Task<TemplateInfoDto> GetTemplateAsync(string id, CancellationToken ct);
    [Post("/api/templates")]
    Task<TemplateInfoDto> CreateTemplateAsync([Body] Core.Models.CreateTemplateRequest request, CancellationToken ct);
    [Put("/api/templates/{id}")]
    Task<TemplateInfoDto> UpdateTemplateAsync(string id, [Body] Core.Models.UpdateTemplateRequest request, CancellationToken ct);
    [Delete("/api/templates/{id}")]
    Task DeleteTemplateAsync(string id, CancellationToken ct);
    [Post("/api/templates/{id}/render")]
    Task<TemplateRenderResultDto> RenderTemplateAsync(string id, [Body] Core.Models.RenderTemplateRequest request, CancellationToken ct);

    // Experiments
    [Get("/api/experiments")]
    Task<List<ExperimentInfoDto>> GetExperimentsAsync(CancellationToken ct);
    [Post("/api/experiments")]
    Task<ExperimentInfoDto> StartExperimentAsync([Body] Core.Models.StartExperimentRequest request, CancellationToken ct);
    [Post("/api/experiments/{id}/complete")]
    Task CompleteExperimentAsync(string id, CancellationToken ct);
    [Post("/api/experiments/{id}/metrics")]
    Task RecordMetricAsync(string id, [Body] Core.Models.RecordMetricRequest request, CancellationToken ct);
    [Get("/api/experiments/{id}/analysis")]
    Task<ExperimentAnalysisDto> GetExperimentAnalysisAsync(string id, CancellationToken ct);
}

// Template DTOs
public sealed record TemplateInfoDto(string Id, string Name, string Description, string Content, string Category, string Version, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record TemplateRenderResultDto(string? RenderedContent, string? Error);

// Experiment DTOs
public sealed record ExperimentInfoDto(string Id, string Name, string Status, string? Description, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt);
public sealed record MetricEntryDto(string Name, double Value, DateTimeOffset Timestamp);
public sealed record ExperimentAnalysisDto(string ExperimentId, int TotalMetrics, double AvgValue, double AvgLatencyMs, double SuccessRate, List<MetricEntryDto> Metrics, List<string> Insights);

public sealed record ApprovalActionDto(string? Comment);
public sealed record ApprovalRequestDto(
    string RequestId, string ActionId, string ActionType, string Description,
    string? PayloadJson, double RiskScore, string[] RequiredApprovers,
    DateTimeOffset CreatedAt, DateTimeOffset Deadline, string Status,
    List<ApprovalResponseDto>? Responses, string? AgentName, string? RequestedBy);
public sealed record ApprovalResponseDto(
    string ApproverId, string ApproverName, bool Approved, string? Comment, DateTimeOffset Timestamp);

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

// Knowledge DTOs
public sealed record KnowledgeQueryResultDto(string Query, List<KnowledgeHitDto>? Hits, int TotalCount);
public sealed record KnowledgeHitDto(string Id, string Content, double Score, string? Source, DateTimeOffset CreatedAt);
public sealed record KnowledgeStatsDto(int TotalEntries, int TotalSources, int QueriesToday, DateTimeOffset LastIndexed);
public sealed record KnowledgeLearnRequestDto(string Content, string Source, string? Category);
public sealed record KnowledgeLearnResultDto(bool Success, string? EntryId, string? Error);

// PIE DTOs
public sealed record PieInferRequestDto(string Premise, string? Context);
public sealed record PieInferResultDto(string Conclusion, double Confidence, List<string>? SupportingEvidence);
public sealed record PieChainRequestDto(string InitialPremise, int Steps, string? Context);
public sealed record PieChainStepDto(int Step, string Premise, string Conclusion, double Confidence);
public sealed record PieChainResultDto(List<PieChainStepDto>? Steps);
public sealed record PieKnowledgeRequestDto(string Domain, string Fact, double Certainty);
public sealed record PieKnowledgeResultDto(bool Success);
public sealed record PieCoherenceResultDto(double OverallCoherence, List<PieCoherenceEntryDto>? Entries);
public sealed record PieCoherenceEntryDto(string Id, string Statement, double CoherenceScore);
public sealed record PieTermDto(string Id, string Name, string? Description, int OccurrenceCount);

// Emotional DTOs
public sealed record EmotionalEventRequestDto(string Event, string? Trigger, double? ValenceDelta, double? ArousalDelta);
public sealed record EmotionalEventResultDto(bool Success);
public sealed record EmotionalHistoryEntryDto(DateTimeOffset Timestamp, string Event, double Valence, double Arousal, string? Trigger);

// Event DTOs
public sealed record EventInfoDto(string EventId, string Type, string Description, string? Source, DateTimeOffset Timestamp, Dictionary<string, object>? Metadata);
public sealed record EventDetailDto(string EventId, string Type, string Description, string? Source, DateTimeOffset Timestamp, Dictionary<string, object>? Metadata, string? RelatedEntityId, string? RelatedEntityType);

// Episodic Memory DTOs
public sealed record EpisodicMemorySearchRequestDto(string UserId, string Query, int TopK = 5);
public sealed record EpisodicMemorySearchResultDto(bool Ok, List<EpisodicMemoryHitDto>? Hits);
public sealed record EpisodicMemoryHitDto(string EpisodeId, string Goal, string Summary, string Status, double? Similarity, DateTimeOffset CreatedAt);
