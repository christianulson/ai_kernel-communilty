namespace Kernel.Contracts;

public sealed record FitnessMetricsSummary(
    double SuccessRate,
    double AvgLatencyMs
);
