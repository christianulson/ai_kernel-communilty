namespace Kernel.Contracts;

public sealed record EvolvableParameter(
    string Name,
    string Domain,
    double CurrentValue,
    double MinValue,
    double MaxValue,
    double StepSize,
    string Description
);

public sealed record ParameterMutation(
    string ParameterName,
    double PreviousValue,
    double NewValue,
    double Delta
);

public sealed record EvolutionAttempt(
    string AttemptId,
    string ParameterName,
    double BaselineValue,
    double MutatedValue,
    double BaselineScore,
    double MutatedScore,
    bool Promoted,
    DateTimeOffset AttemptedAt
);

public sealed record PromotionCriteria(
    int MinEvaluationRuns,
    double MinImprovementThreshold,
    double MaxLatencyRegression,
    bool RequireBenchmarkPass
);

public sealed record PromotionDecision(
    bool Approved,
    string ParameterName,
    double CurrentValue,
    double ProposedValue,
    double Improvement,
    IReadOnlyList<string> CheckResults,
    string? RejectionReason
);

public sealed record RollbackRecord(
    string ParameterName,
    double ValueBeforeRollback,
    double ValueAfterRollback,
    string Reason,
    DateTimeOffset RolledbackAt
);

public sealed record RollbackResult(
    bool RollbackTriggered,
    string? Reason,
    double? PreviousValue,
    double? RestoredValue
);

public sealed record EvolutionCycleResult(
    string ParameterName,
    double BaselineValue,
    double? MutatedValue,
    double BaselineScore,
    double MutatedScore,
    bool Promoted,
    IReadOnlyList<string> Steps
);

public sealed record EvolutionState(
    IReadOnlyList<EvolvableParameter> Parameters,
    int TotalCycles,
    int TotalPromotions,
    int TotalRollbacks
);
