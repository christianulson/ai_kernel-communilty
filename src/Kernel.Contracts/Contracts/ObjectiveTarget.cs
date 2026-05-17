namespace Kernel.Contracts;

public sealed record ObjectiveTarget(
    string TargetId,
    string ObjectiveId,
    string Name,
    double CurrentValue,
    double TargetValue,
    string Unit,
    string Direction,
    double Weight,
    DateTimeOffset? Deadline,
    string Status,
    DateTimeOffset? LastMeasuredAt);
