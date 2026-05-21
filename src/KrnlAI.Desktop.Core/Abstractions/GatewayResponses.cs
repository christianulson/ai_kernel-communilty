namespace KrnlAI.Desktop.Core.Abstractions;

public sealed record HealthResponse(bool Ok, DateTimeOffset Ts);

public sealed record AgentRunResponseDto(
    string? Narration,
    Dictionary<string, object>? Command,
    List<TransportStepDto>? TransportSteps,
    List<string>? ActiveStages,
    string? Error);

public sealed record TransportStepDto(string Label, string Detail, bool Ok, int? Status);

public sealed record TtsResponseDto(string? Base64, string? MimeType);
public sealed record TranscriptionResponseDto(string? Text);

public sealed record LoginResponseDto(bool Success, string? Token, string? Message, string? Username, DateTime? ExpiresAt);

public sealed record PolicyListResponseDto(List<PolicyInfoDto>? Policies, int TotalCount, int Page, int PageSize);
public sealed record PolicyInfoDto(string Id, string Name, string Domain, string Version, DateTime CreatedAt, DateTime? UpdatedAt, bool IsActive);
public sealed record PolicyDetailsDto(string Id, string Name, string Domain, string Version, string? Content, DateTime CreatedAt, DateTime? UpdatedAt, bool IsActive, List<PolicyVersionDto>? Versions);
public sealed record PolicyVersionDto(string Version, DateTime CreatedAt, string CreatedBy, string? ChangeNote);
public sealed record PolicyVersionExtendedDto(string PolicyId, string Version, DateTime CreatedAt, string CreatedBy, string? ChangeNote, double? SuccessRate);
public sealed record PolicyVersionListDto(List<PolicyVersionExtendedDto>? Versions);
public sealed record PolicyRollbackDto(string RollbackId, string PolicyId, string TargetVersion, string PerformedBy, string Reason, DateTime PerformedAt);

public sealed record EpisodeSearchResultDto(List<EpisodeInfoDto>? Episodes, int TotalCount, int Page, int PageSize);
public sealed record EpisodeInfoDto(string Id, string GoalId, string Status, DateTime CreatedAt, DateTime? FinishedAt, int? DurationMs, string? Outcome, double? SuccessRate);
public sealed record EpisodeDetailsDto(string Id, string GoalId, string Status, DateTime CreatedAt, DateTime? FinishedAt, int? DurationMs, string? Outcome, double? SuccessRate, string? Summary, List<EpisodeStepDto>? Steps);
public sealed record EpisodeStepDto(int StepIndex, string Label, string Detail, DateTime? StartedAt, DateTime? FinishedAt, int? DurationMs, bool Ok, string? Error);

public sealed record MemorySearchResultDto(List<MemoryHitDto>? Hits, int TotalCount, double QueryTimeMs);
public sealed record MemoryHitDto(string Id, string Content, string Source, double Score, DateTime CreatedAt, Dictionary<string, string>? Metadata);
public sealed record MemoryIngestResultDto(bool Success, string? DocumentId, int ChunksCreated, string? Error);
public sealed record MemoryMetricsDto(int TotalChunks, int TotalDocuments, long TotalSizeBytes, Dictionary<string, int>? BySource, DateTime? OldestEntry, DateTime? NewestEntry);
public sealed record WorkingMemoryDto(int ActiveSlots, int MaxSlots, List<WorkingMemorySlotDto>? Slots);
public sealed record WorkingMemorySlotDto(string Key, string Content, double Relevance, DateTime CreatedAt, DateTime? ExpiresAt);

public sealed record AgentMetricsDto(int TotalRuns, int CompletedRuns, int FailedRuns, int AbortedRuns, double SuccessRate, double AvgLatencyMs, double AvgCost, Dictionary<string, GoalMetricsDto>? ByGoal);
public sealed record GoalMetricsDto(string GoalId, int TotalRuns, int CompletedRuns, int FailedRuns, double SuccessRate, double AvgLatencyMs, double AvgEstimatedCost);
public sealed record ScorecardDto(double Reliability, double Efficiency, double Safety, double AntiLoop, double Governance, double Overall);
public sealed record RuntimeSummaryDto(bool GatewayHealthy, bool KernelHealthy, string? KernelVersion, string? GatewayVersion, int ActiveGoals, long MemoryUsageBytes, Dictionary<string, string>? Services);

public sealed record GoalListDto(List<GoalInfoDto>? Goals, int TotalCount);
public sealed record GoalInfoDto(string GoalId, string Description, string Status, int Priority, DateTime CreatedAt, DateTime? CompletedAt, DateTime? Deadline, double? SuccessRate, int SubGoalCount, int CompletedSubGoals);
public sealed record GoalDetailsDto(string GoalId, string Description, string Status, int Priority, DateTime CreatedAt, DateTime? CompletedAt, DateTime? Deadline, double? SuccessRate, List<SubGoalDto>? SubGoals, List<GoalCycleDto>? Cycles);
public sealed record SubGoalDto(string Id, string Description, bool Completed);
public sealed record GoalCycleDto(string Action, string Status, int DurationMs, DateTime Timestamp, string? GoalId);

public sealed record FeedbackResultDto(bool Success, string? FeedbackId, string? Message);

public sealed record CognitiveDashboardDto(double OverallHealth, List<CognitiveModuleDto>? ActiveModules, List<CognitiveEventDto>? RecentEvents, AutonomyStatusDto? Autonomy);
public sealed record CognitiveModuleDto(string Name, double HealthScore, string Status);
public sealed record CognitiveEventDto(string Type, string Description, string Source, DateTime Timestamp);
public sealed record AutonomyStatusDto(string Level, DateTime LastUpdated, Dictionary<string, double>? DomainConfidence);

public sealed record UserProfileDto(string UserId, string? Name, string? Email, string? Role, Dictionary<string, string>? Preferences, DateTime? CreatedAt);

public sealed record MultimodalSearchResultDto(List<MultimodalHitDto>? Hits);
public sealed record MultimodalHitDto(string Id, string Content, string Modality, double Score, string? ThumbnailBase64);

public sealed record BenchmarkDto(int TotalSuites, int TotalScenarios, double OverallScore, double AvgLatencyMs, double AvgSuccessRate, List<BenchmarkSuiteDto>? Suites);
public sealed record BenchmarkSuiteDto(string Name, int Scenarios, double Score, double LatencyMs, double SuccessRate);

public sealed record CausalQueryDto(List<CausalNodeDto>? Nodes, List<CausalEdgeDto>? Edges);
public sealed record CausalNodeDto(string Id, string Label, string Type, Dictionary<string, double>? Attributes);
public sealed record CausalEdgeDto(string SourceId, string TargetId, string Label, double Weight);
public sealed record CausalPredictionDto(string Action, string Outcome, double Probability, List<string>? ContributingFactors);

public sealed record CrossSummaryDto(CrossServiceStatusDto? Gateway, CrossServiceStatusDto? Kernel, HybridWeightsDto? HybridWeights);
public sealed record CrossServiceStatusDto(string? Version, TimeSpan? Uptime, int ActiveLimiters, int RequestsPerMinute, int ConnectedStores, int TotalStores);
public sealed record HybridWeightsDto(double Semantic, double Lexical, double Recency, double Confidence);

public sealed record EmotionalStateDto(double Valence, double Arousal, double Motivation, DateTimeOffset UpdatedAt);

public sealed record PainSignalDto(string SignalId, string Category, string Source, string Description, double Intensity, DateTimeOffset OccurredAt);

public sealed record RewardSignalDto(string SignalId, string Category, string Source, string Description, double Value, DateTimeOffset OccurredAt);

public sealed record AffectiveStateDto(double Valence, double Arousal, double PainLevel, double RewardLevel, List<PainSignalDto>? RecentPains, List<RewardSignalDto>? RecentRewards, DateTimeOffset UpdatedAt);

public sealed record MetricsByGoalDto(List<GoalMetricsDto>? Goals, int TotalCount);
public sealed record GoalCycleListDto(List<GoalCycleSummaryDto>? Cycles);
public sealed record GoalCycleSummaryDto(string GoalId, string Action, string Status, DateTime Timestamp, int DurationMs);

public sealed record DocumentInfoDto(string DocumentId, string FileName, long FileSize, string Format, string Status, string? ErrorMessage, int ChunkCount, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt);
public sealed record DocumentSearchResponseDto(bool Ok, List<DocumentSearchHitDto>? Hits);
public sealed record DocumentSearchHitDto(string ChunkId, string? DocId, string Text, double Score);
public sealed record DocumentUploadResponseDto(string DocumentId, string Status, string StatusUrl);

public sealed record ArchiveStatsDto(bool Ok, int TotalArchived, List<string>? Stores);
public sealed record ModelRegistryDetailDto(string ModelId, List<ModelRegistryEntryDto>? Models, ModelRegistryEntryDto? Active);
public sealed record ModelRegistryEntryDto(string ModelId, string ModelVersion, string UseCase, string Runtime, string Status, string? ApprovedBy, DateTimeOffset CreatedAt, DateTimeOffset? ActivatedAt);
public sealed record VersionsInfoDto(string DefaultVersion, List<string>? SupportedVersions, bool LegacyUnversionedDeprecated, string LegacySunsetDate);
public sealed record ContractsResponseDto(string DefaultApiVersion, List<ContractEntryDto>? Contracts);
public sealed record ContractEntryDto(string Endpoint, string ContractVersion, string SupportedRange, bool Deprecated, string State);
public sealed record ShareListResponseDto(List<ShareDto>? Shares);
public sealed record ShareDto(string ShareCode, string SessionId, string AccessLevel, DateTime CreatedAt, DateTime? ExpiresAt, int AccessCount, bool IsRevoked);

public sealed record RefreshTokenRequest(string? RefreshToken);
public sealed record RefreshTokenResponseDto(string? Token, string? RefreshToken, string? Error);
public sealed record OAuth2CallbackRequest(string Code, string State, string Provider);
public sealed record OAuth2LoginRequest(string Provider, string RedirectUri, string State);
public sealed record OAuth2LoginResponse(string? AuthUrl, string? Error);
