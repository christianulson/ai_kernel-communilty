namespace Kernel.Contracts;

public sealed record BenchmarkScenario(
    string ScenarioId,
    string Goal,
    string Domain,
    string[] ExpectedTools,
    int MinSteps,
    int MaxSteps,
    string[] RequiredOutcomes,
    string MaxRiskLevel
)
{
    private static readonly string[] AllowedRiskLevels = ["low", "medium", "high"];

    public BenchmarkScenarioValidationResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ScenarioId))
        {
            errors.Add("scenarioId is required");
        }

        if (string.IsNullOrWhiteSpace(Goal))
        {
            errors.Add("goal is required");
        }

        if (string.IsNullOrWhiteSpace(Domain))
        {
            errors.Add("domain is required");
        }

        if (ExpectedTools.Length == 0 || ExpectedTools.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add("expectedTools must include at least one tool");
        }

        if (MinSteps <= 0)
        {
            errors.Add("minSteps must be greater than zero");
        }

        if (MaxSteps < MinSteps)
        {
            errors.Add("maxSteps must be greater than or equal to minSteps");
        }

        if (RequiredOutcomes.Length == 0 || RequiredOutcomes.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add("requiredOutcomes must include at least one outcome");
        }

        if (!AllowedRiskLevels.Contains(MaxRiskLevel, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add("maxRiskLevel must be one of: low, medium, high");
        }

        return new BenchmarkScenarioValidationResult(errors.Count == 0, errors.AsReadOnly());
    }
}

public sealed record BenchmarkScenarioValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors
);

public sealed record BenchmarkResult(
    string ScenarioId,
    string Goal,
    bool Passed,
    double Score,
    IReadOnlyList<string> PassedChecks,
    IReadOnlyList<string> FailedChecks,
    string? FailureReason
)
{
    public IReadOnlyDictionary<string, double> CognitiveMetrics { get; init; } =
        new Dictionary<string, double>();
}

public sealed record BenchmarkSuiteResult(
    string SuiteName,
    DateTimeOffset ExecutedAt,
    int TotalScenarios,
    int PassedScenarios,
    double OverallScore,
    IReadOnlyList<BenchmarkResult> Results
);

public sealed record BenchmarkDashboardSummary(
    DateTimeOffset ExecutedAt,
    int TotalScenarios,
    int PassedScenarios,
    double OverallScore,
    IReadOnlyList<BenchmarkSuiteScore> SuiteScores,
    double? MetacognitiveConfidenceDelta = null,
    double? MetacognitiveEntropyDelta = null,
    int? MetacognitiveReplaySuccesses = null,
    int? MetacognitiveReplayFailures = null
);

public sealed record BenchmarkSuiteScore(
    string SuiteName,
    int TotalScenarios,
    int PassedScenarios,
    double Score
);

public sealed record BenchmarkGateConfig(
    double MinSuccessRate,
    int MinScenarios,
    IReadOnlyList<string> CriticalScenarioIds
)
{
    public IReadOnlyDictionary<string, double> MinCognitiveMetrics { get; init; } =
        new Dictionary<string, double>();

    public double? SensoryRiskScore { get; init; }

    public double SensoryRiskThreshold { get; init; } = 0.60;
}

public sealed record BenchmarkGateResult(
    bool Passed,
    double SuccessRate,
    int TotalScenarios,
    int PassedScenarios,
    IReadOnlyList<string> FailedScenarioIds,
    string? FailureReason
);
