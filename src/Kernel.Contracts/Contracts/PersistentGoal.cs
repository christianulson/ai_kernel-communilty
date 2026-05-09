namespace Kernel.Contracts;

public record PersistentGoal(
    string GoalId,
    string? ParentGoalId,
    string Description,
    string Status,           // "active", "paused", "completed", "failed", "abandoned"
    double Progress,         // 0-1
    double Priority,
    DateTimeOffset CreatedAt,
    DateTimeOffset? Deadline,
    IReadOnlyList<string> SubGoalIds,
    IReadOnlyList<string> DependsOnGoalIds,
    IReadOnlyDictionary<string, string> SuccessMetrics
);
