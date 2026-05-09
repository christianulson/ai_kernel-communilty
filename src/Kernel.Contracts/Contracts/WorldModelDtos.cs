namespace Kernel.Contracts;

/// <summary>
/// Crença mínima do World Model.
/// </summary>
public sealed record Belief(
    string Subject,
    string Predicate,
    string Object,
    double Confidence,
    DateTime LastUpdatedUtc,
    string Source = "agent",
    DateTimeOffset? ValidFrom = null,
    DateTimeOffset? ValidUntil = null,
    IReadOnlyList<string>? Contradicts = null,
    IReadOnlyList<string>? EvidenceEpisodeIds = null);

/// <summary>
/// Métricas objetivas de execução do agente (por episódio).
/// </summary>
public sealed record AgentRunMetrics(
    string UserId,
    string? EpisodeId,
    string GoalHash,
    string? GoalId,
    bool Success,
    int PlannedSteps,
    int ExecutedSteps,
    int RiskIncidents,
    bool LoopDetected,
    long LatencyMs,
    double EstimatedCost,
    DateTimeOffset? CreatedAtUtc = null
);


/// <summary>
/// Resumo agregado de métricas para observabilidade operacional.
/// </summary>
public sealed record AgentMetricsSummary(
    int TotalRuns,
    int SuccessfulRuns,
    double SuccessRate,
    double AvgLatencyMs,
    double AvgEstimatedCost
);


/// <summary>
/// Resumo agregado de métricas agrupado por GoalId para visão de dashboard.
/// </summary>
public sealed record AgentMetricsByGoalSummary(
    string GoalId,
    int TotalRuns,
    int SuccessfulRuns,
    double SuccessRate,
    double AvgLatencyMs,
    double AvgEstimatedCost
);
