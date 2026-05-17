namespace Kernel.Contracts;

public sealed record ObjectiveProgressReport(
    string ObjectiveId,
    double WeightedProgress,
    IReadOnlyList<TargetProgressItem> TargetProgress,
    string Source);

public sealed record TargetProgressItem(
    string TargetId,
    string Name,
    double CurrentValue,
    double TargetValue,
    double Progress,
    string Direction,
    double Weight,
    string Status);
