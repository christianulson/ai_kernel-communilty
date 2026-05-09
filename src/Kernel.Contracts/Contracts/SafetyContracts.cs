namespace Kernel.Contracts;

public sealed record SafetyCase(
    string RequiredApprover,
    string RollbackPlan,
    IReadOnlyList<string> Evidence);

public sealed record HighRiskSimulationResult(
    double RiskScore,
    double ExpectedSuccessProbability,
    bool RequiresApproval,
    IReadOnlyList<string> Concerns,
    SafetyCase? SafetyCase = null);
