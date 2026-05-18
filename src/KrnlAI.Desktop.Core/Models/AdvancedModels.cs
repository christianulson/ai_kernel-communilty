namespace KrnlAI.Desktop.Core.Models;

public record CausalNode(
    string Id, string Label, string Type,
    Dictionary<string, double>? Attributes
);

public record CausalEdge(
    string SourceId, string TargetId, string Label, double Weight
);

public record CausalPrediction(
    string Action, string Outcome, double Probability,
    List<string>? ContributingFactors
);

public record CausalQueryResult(
    string Query, List<CausalNode> Nodes, List<CausalEdge> Edges
);

public record BenchmarkSummary(
    int TotalSuites, int TotalScenarios, double OverallScore,
    double AvgLatencyMs, double AvgSuccessRate,
    List<BenchmarkSuite> Suites
);

public record BenchmarkSuite(
    string Name, int Scenarios, double Score,
    double LatencyMs, double SuccessRate
);

public record UserProfile(
    string UserId, string? Name, string? Email, string? Role,
    Dictionary<string, string>? Preferences, DateTime? CreatedAt
);

public record CognitiveDashboardData(
    double OverallHealth, List<CognitiveModule> ActiveModules,
    List<CognitiveEvent> RecentEvents, AutonomyStatus? Autonomy
);

public record CognitiveModule(string Name, double HealthScore, string Status);

public record CognitiveEvent(
    string Type, string Description, string Source, DateTime Timestamp
);

public record AutonomyStatus(
    string Level, DateTime LastUpdated, Dictionary<string, double>? DomainConfidence
);

public record MultimodalSearchResult(
    string Query, List<MultimodalHit> Hits
);

public record MultimodalHit(
    string Id, string Content, string Modality, double Score,
    string? ThumbnailBase64
);

public record CrossSummaryData(
    CrossServiceStatus Gateway, CrossServiceStatus Kernel,
    HybridWeightsData? HybridWeights
);

public record CrossServiceStatus(
    string? Version, TimeSpan? Uptime,
    int ActiveLimiters, int RequestsPerMinute,
    int ConnectedStores, int TotalStores
);

public record HybridWeightsData(
    double Semantic, double Lexical, double Recency, double Confidence
);

public record MetricsByGoalData(
    List<GoalMetrics> Goals, int TotalCount
);

public record PolicyVersionList(
    List<PolicyVersionExtended> Versions
);

public record PolicyVersionExtended(
    string PolicyId, string Version, DateTime CreatedAt,
    string CreatedBy, string? ChangeNote, double? SuccessRate
);

public record PolicyRollbackEntry(
    string RollbackId, string PolicyId, string TargetVersion,
    string PerformedBy, string Reason, DateTime PerformedAt
);

public record GoalCycleList(
    List<GoalCycleSummary> Cycles
);

public record GoalCycleSummary(
    string GoalId, string Action, string Status,
    DateTime Timestamp, int DurationMs
);
