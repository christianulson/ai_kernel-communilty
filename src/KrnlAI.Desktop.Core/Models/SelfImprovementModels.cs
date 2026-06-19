namespace KrnlAI.Desktop.Core.Models;

public sealed record SelfImprovementStatus(
    bool IsRunning,
    bool IsEnabled,
    DateTimeOffset? LastCycleAt,
    int TotalCycles,
    int SuccessfulCycles,
    int FailedCycles,
    List<CycleInfo> RecentCycles,
    List<TraceInfo> RecentTraces);

public sealed record CycleInfo(
    string CycleId,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string Status,
    string? Summary,
    double? FitnessDelta);

public sealed record TraceInfo(
    string TraceId,
    string Message,
    string Level,
    string Source,
    DateTimeOffset Timestamp);
