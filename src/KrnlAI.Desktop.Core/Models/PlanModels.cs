namespace KrnlAI.Desktop.Core.Models;

public enum PlanStatus { NotStarted, InProgress, Completed, Failed, Cancelled }

public enum PlanStepStatus { Pending, InProgress, Completed, Failed, Skipped }

public record PlanInfo(
    string Id,
    string Goal,
    string Description,
    PlanStatus Status,
    double Progress,
    int TotalSteps,
    int CompletedSteps,
    DateTime CreatedAt,
    DateTime? EstimatedCompletion
);

public record PlanStep(
    int Index,
    string Description,
    string Detail,
    PlanStepStatus Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? Result
);

public record PlanExecutionResult(
    string PlanId,
    bool IsRunning,
    PlanInfo? CurrentPlan,
    List<PlanStep> Steps,
    string? Error
);
