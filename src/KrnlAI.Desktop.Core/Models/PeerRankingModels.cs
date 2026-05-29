namespace KrnlAI.Desktop.Core.Models;

public sealed record PeerRankingItem(
    string NodeId,
    string Tier,
    double OverallScore,
    double SuccessRateScore,
    double LatencyScore,
    double AvailabilityScore,
    double TenureScore,
    double CapacityScore,
    double CatalogScore,
    int TotalJobsExecuted,
    int TotalJobsFailed,
    double AvgResponseTimeMs,
    double UptimePercentage,
    DateTime FirstSeen,
    DateTime LastSeen,
    int QuarantineCount);

public sealed record PeerRankingHistoryEntry(
    string NodeId,
    string EventType,
    double OverallScore,
    string Tier,
    double Delta,
    string? Reason,
    DateTime Timestamp);

public sealed record PeerRankingStrategyState(
    string CurrentStrategyName,
    IReadOnlyList<string> AvailableStrategies);

public sealed record PeerRankingWeights(
    double SuccessRateWeight,
    double LatencyWeight,
    double AvailabilityWeight,
    double TenureWeight,
    double CapacityWeight,
    double CatalogWeight);
