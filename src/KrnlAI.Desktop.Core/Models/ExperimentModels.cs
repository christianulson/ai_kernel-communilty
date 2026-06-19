namespace KrnlAI.Desktop.Core.Models;

/// <summary>Information about an experiment.</summary>
public sealed record ExperimentInfo(
    string Id,
    string Name,
    string Status,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

/// <summary>Request to start a new experiment.</summary>
public sealed record StartExperimentRequest(
    string Name,
    string? Description = null,
    Dictionary<string, string>? Parameters = null);

/// <summary>Request to record a metric for an experiment.</summary>
public sealed record RecordMetricRequest(
    string MetricName,
    double Value,
    Dictionary<string, string>? Tags = null);

/// <summary>Analysis results for an experiment.</summary>
public sealed record ExperimentAnalysis(
    string ExperimentId,
    int TotalMetrics,
    double AvgValue,
    double AvgLatencyMs,
    double SuccessRate,
    List<MetricEntry> Metrics,
    List<string> Insights);

/// <summary>A single metric entry in experiment analysis.</summary>
public sealed record MetricEntry(
    string Name,
    double Value,
    DateTimeOffset Timestamp);
