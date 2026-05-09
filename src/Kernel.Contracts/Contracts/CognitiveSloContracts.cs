namespace Kernel.Contracts;

/// <summary>
/// Snapshot of cognitive SLO signals for a given evaluation.
/// </summary>
/// <param name="SuccessRate">Observed success rate from 0 to 1.</param>
/// <param name="SafetyScore">Observed safety score from 0 to 1.</param>
/// <param name="EstimatedCostUsd">Estimated cost in USD.</param>
/// <param name="P95LatencyMs">Observed p95 latency in milliseconds.</param>
public sealed record CognitiveSloSnapshot(
    double SuccessRate,
    double SafetyScore,
    decimal EstimatedCostUsd,
    double P95LatencyMs);

/// <summary>
/// Violation raised by a cognitive SLO validator.
/// </summary>
/// <param name="MetricName">Metric that violated the threshold.</param>
/// <param name="ActualValue">Observed value.</param>
/// <param name="ThresholdValue">Configured threshold.</param>
/// <param name="Unit">Display unit.</param>
public sealed record CognitiveSloViolation(
    string MetricName,
    double ActualValue,
    double ThresholdValue,
    string Unit);

/// <summary>
/// Result of a cognitive SLO validation.
/// </summary>
/// <param name="IsHealthy">Whether all SLOs are within threshold.</param>
/// <param name="Score">Normalized score derived from the metrics.</param>
/// <param name="Violations">List of violated thresholds.</param>
public sealed record CognitiveSloValidationResult(
    bool IsHealthy,
    double Score,
    IReadOnlyList<CognitiveSloViolation> Violations);
