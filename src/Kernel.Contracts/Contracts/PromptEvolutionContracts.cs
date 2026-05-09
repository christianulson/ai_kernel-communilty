namespace Kernel.Contracts;

/// <summary>
/// Baseline prompt snapshot used as a golden reference.
/// </summary>
/// <param name="PromptCapability">Prompt capability name.</param>
/// <param name="PromptVersion">Version of the baseline prompt.</param>
/// <param name="GoldenPromptHash">Stable hash of the golden prompt.</param>
/// <param name="PromptText">Baseline prompt text.</param>
public sealed record PromptEvolutionBaseline(
    string PromptCapability,
    string PromptVersion,
    string GoldenPromptHash,
    string PromptText);

/// <summary>
/// Candidate prompt snapshot proposed by the evolution loop.
/// </summary>
/// <param name="PromptCapability">Prompt capability name.</param>
/// <param name="PromptVersion">Version of the candidate prompt.</param>
/// <param name="GoldenPromptHash">Stable hash of the golden prompt.</param>
/// <param name="PromptText">Candidate prompt text.</param>
/// <param name="Benchmark">Offline benchmark result for the candidate.</param>
public sealed record PromptEvolutionCandidate(
    string PromptCapability,
    string PromptVersion,
    string GoldenPromptHash,
    string PromptText,
    BenchmarkGateResult Benchmark)
{
    public double? SensoryRiskScore { get; init; }
}

/// <summary>
/// Result of evaluating a prompt evolution candidate.
/// </summary>
/// <param name="Approved">Whether the candidate is approved.</param>
/// <param name="RejectionReason">Rejection reason, if any.</param>
/// <param name="BaselinePromptVersion">Baseline version.</param>
/// <param name="CandidatePromptVersion">Candidate version.</param>
/// <param name="BenchmarkPassed">Whether the benchmark passed.</param>
/// <param name="GoldenPromptRegressed">Whether golden prompt content regressed.</param>
public sealed record PromptEvolutionResult(
    bool Approved,
    string? RejectionReason,
    string BaselinePromptVersion,
    string CandidatePromptVersion,
    bool BenchmarkPassed,
    bool GoldenPromptRegressed);
