namespace KrnlAI.Desktop.Core.Models;

public record GoalInfo(
    string GoalId,
    string Description,
    string Status,
    int Priority,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    DateTime? Deadline,
    double? SuccessRate,
    int SubGoalCount,
    int CompletedSubGoals
);

public record GoalDetails(
    string GoalId,
    string Description,
    string Status,
    int Priority,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    DateTime? Deadline,
    double? SuccessRate,
    List<SubGoal>? SubGoals,
    List<GoalCycle>? Cycles
);

public record SubGoal(
    string Id,
    string Description,
    bool Completed
);

public record GoalCycle(
    string Action,
    string Status,
    int DurationMs,
    DateTime Timestamp,
    string? GoalId
);

public record CreateGoalRequest(
    string Description,
    int Priority = 3,
    DateTime? Deadline = null
);

public record GoalListResponse(
    List<GoalInfo> Goals,
    int TotalCount
);
