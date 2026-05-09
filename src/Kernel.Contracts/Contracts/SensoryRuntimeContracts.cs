namespace Kernel.Contracts;

/// <summary>
/// Short time-series point for the sensory runtime dashboard.
/// </summary>
public sealed record SensoryRuntimeTimeSeriesPoint(
    DateTimeOffset Timestamp,
    string ScenarioLabel,
    double DemandScore,
    int AcceptedSignals,
    int RejectedSignals,
    bool ShouldTriggerCycle);
