namespace KrnlAI.VisualStudio.Services;

public sealed record DashboardScorecard(
    double GoalAutonomy,
    double ExecutionAutonomy,
    double SafetyAutonomy,
    double LearningAutonomy,
    double MetaCognitionAutonomy
);

public sealed record SystemHealth(
    string Status,
    string? Version,
    string? Uptime,
    double? LatencyMs
);

public interface IDashboardService
{
    Task<DashboardScorecard?> GetScorecardAsync(CancellationToken ct);
    Task<SystemHealth?> GetHealthAsync(CancellationToken ct);
    Task<string?> GetMoodAsync(CancellationToken ct);
}
