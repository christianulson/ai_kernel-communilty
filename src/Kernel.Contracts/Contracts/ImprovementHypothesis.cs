namespace Kernel.Contracts;

/// <summary>
/// Canonical improvement hypothesis used by the controlled self-improvement loop.
/// </summary>
/// <param name="HypothesisId">Stable identifier for the hypothesis.</param>
/// <param name="Title">Short human-readable title.</param>
/// <param name="ProblemStatement">Problem the hypothesis intends to fix.</param>
/// <param name="ProposedChange">Concrete change proposed.</param>
/// <param name="ExpectedMetric">Primary metric expected to improve.</param>
/// <param name="Evidence">Evidence supporting the hypothesis.</param>
/// <param name="ExpectedImprovement">Expected improvement in the target metric.</param>
/// <param name="RiskLevel">Expected risk level of the change.</param>
public sealed record ImprovementHypothesis(
    string HypothesisId,
    string Title,
    string ProblemStatement,
    string ProposedChange,
    string ExpectedMetric,
    IReadOnlyList<string> Evidence,
    double ExpectedImprovement,
    string RiskLevel)
{
    private static readonly string[] AllowedRiskLevels = ["low", "medium", "high"];

    /// <summary>
    /// Validates the hypothesis structure.
    /// </summary>
    /// <returns>Validation result with errors if required fields are missing.</returns>
    public ImprovementHypothesisValidationResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(HypothesisId))
        {
            errors.Add("hypothesis_id_is_required");
        }

        if (string.IsNullOrWhiteSpace(Title))
        {
            errors.Add("title_is_required");
        }

        if (string.IsNullOrWhiteSpace(ProblemStatement))
        {
            errors.Add("problem_statement_is_required");
        }

        if (string.IsNullOrWhiteSpace(ProposedChange))
        {
            errors.Add("proposed_change_is_required");
        }

        if (string.IsNullOrWhiteSpace(ExpectedMetric))
        {
            errors.Add("expected_metric_is_required");
        }

        if (Evidence.Count == 0 || Evidence.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add("evidence_is_required");
        }

        if (!AllowedRiskLevels.Contains(RiskLevel, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add("risk_level_must_be_one_of: low, medium, high");
        }

        if (ExpectedImprovement < 0)
        {
            errors.Add("expected_improvement_must_be_non_negative");
        }

        return new ImprovementHypothesisValidationResult(errors.Count == 0, errors.AsReadOnly());
    }
}

/// <summary>
/// Validation result for an improvement hypothesis.
/// </summary>
/// <param name="IsValid">Whether the hypothesis is valid.</param>
/// <param name="Errors">Validation errors, if any.</param>
public sealed record ImprovementHypothesisValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors);
