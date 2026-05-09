namespace Kernel.Contracts;

/// <summary>
/// Budget for a controlled self-improvement loop.
/// </summary>
/// <param name="MaxCycles">Maximum number of candidates that may be processed.</param>
/// <param name="MaxEstimatedCost">Maximum estimated cost allowed for the loop.</param>
/// <param name="MaxScopeSteps">Maximum cumulative scope steps allowed for the loop.</param>
public sealed record SelfImprovementLoopBudget(
    int MaxCycles,
    decimal MaxEstimatedCost,
    int MaxScopeSteps);

/// <summary>
/// Single candidate step processed by the self-improvement loop.
/// </summary>
/// <param name="CandidateId">Candidate identifier.</param>
/// <param name="EstimatedCost">Estimated cost for the step.</param>
/// <param name="ScopeSteps">Scope size of the step.</param>
public sealed record SelfImprovementLoopStep(
    string CandidateId,
    decimal EstimatedCost,
    int ScopeSteps);

/// <summary>
/// Report produced by the controlled self-improvement loop.
/// </summary>
/// <param name="Stopped">Whether execution stopped before consuming all requested steps.</param>
/// <param name="StopReason">Reason for stopping, if any.</param>
/// <param name="ExecutedCycles">How many steps were executed.</param>
/// <param name="EstimatedCost">Accumulated estimated cost.</param>
/// <param name="ScopeSteps">Accumulated scope steps.</param>
/// <param name="ExecutedCandidateIds">Executed candidate identifiers.</param>
public sealed record SelfImprovementLoopReport(
    bool Stopped,
    string? StopReason,
    int ExecutedCycles,
    decimal EstimatedCost,
    int ScopeSteps,
    IReadOnlyList<string> ExecutedCandidateIds);
