namespace KrnlAI.Desktop.Core.Models;

public record AgentMetricsSummary(
    int TotalRuns,
    int CompletedRuns,
    int FailedRuns,
    int AbortedRuns,
    double SuccessRate,
    double AvgLatencyMs,
    double AvgCost,
    Dictionary<string, GoalMetrics> ByGoal
);

public record GoalMetrics(
    string GoalId,
    int TotalRuns,
    int CompletedRuns,
    int FailedRuns,
    double SuccessRate,
    double AvgLatencyMs,
    double AvgEstimatedCost
);

public record AgentScorecard(
    double Reliability,
    double Efficiency,
    double Safety,
    double AntiLoop,
    double Governance,
    double Overall
);

public record RuntimeSummary(
    bool GatewayHealthy,
    bool KernelHealthy,
    string? KernelVersion,
    string? GatewayVersion,
    int ActiveGoals,
    long MemoryUsageBytes,
    Dictionary<string, string> Services
);

public record ObservabilitySummary(
    RuntimeSummary Runtime,
    AgentScorecard Scorecard,
    AgentMetricsSummary Metrics
);

public record AffectiveState(
    double Valence,
    double Arousal,
    double PainLevel,
    double RewardLevel,
    DateTimeOffset UpdatedAt
);