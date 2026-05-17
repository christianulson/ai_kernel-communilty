namespace Kernel.Contracts;

public sealed record PersistentObjective(
    string ObjectiveId,
    string? PurposeId,
    string? ParentObjectiveId,
    string Description,
    string Status,
    double Progress,
    string Priority,
    DateTimeOffset CreatedAt,
    DateTimeOffset? Deadline,
    IReadOnlyList<string> DependsOnObjectiveIds,
    IReadOnlyList<string> TargetIds,
    string? LegacyGoalId);
