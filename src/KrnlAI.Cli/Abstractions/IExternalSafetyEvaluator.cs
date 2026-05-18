namespace KrnlAI.Cli.Abstractions;

public sealed record SafetyEvaluationResult(
    string ScenarioId,
    bool Blocked,
    string RiskLevel,
    long DurationMs);

public interface IExternalSafetyEvaluator
{
    string Name { get; }
    Task<SafetyEvaluationResult> EvaluateAsync(string prompt, string scenarioId, CancellationToken ct = default);
}