namespace Kernel.Contracts;

public record SimulationResult(
    string PlanId,
    double ExpectedSuccessProbability,
    double ExecutionRisk,
    IReadOnlyList<string> AnticipatedOutcomes,
    IReadOnlyList<string> PotentialRisks
);
