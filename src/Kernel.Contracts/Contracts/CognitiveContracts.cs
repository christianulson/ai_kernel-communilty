namespace Kernel.Contracts;

public sealed record GoalCycleResult(
    bool HasGoal,
    string? SelectedGoalId,
    double? PreviousProgress,
    double? NewProgress,
    string? PreviousStatus,
    string NewStatus,
    int GoalsEvaluated,
    bool Completed,
    DateTimeOffset SelectedAt);
