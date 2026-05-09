namespace Kernel.Contracts;

public sealed record CreateGoalRequest(
    string Description,
    double Priority,
    DateTimeOffset? Deadline = null,
    string? ParentGoalId = null
);
