using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.Services;

/// <summary>
/// No-op ranking service used when the desktop app runs in local mode.
/// </summary>
public sealed class NullPeerRankingManagementService : IPeerRankingManagementService
{
    private static readonly IReadOnlyList<PeerRankingItem> EmptyPeers = [];
    private static readonly IReadOnlyList<PeerRankingHistoryEntry> EmptyHistory = [];
    private static readonly PeerRankingWeights DefaultWeights = new(0.35, 0.20, 0.20, 0.05, 0.10, 0.10);
    private static readonly PeerRankingStrategyState DefaultStrategy = new("TopRanked", ["TopRanked", "WeightedRandom", "TopRankedWithFallback", "ByTier", "LeastLoaded"]);

    public Task<IReadOnlyList<PeerRankingItem>> GetRankingAsync(CancellationToken ct = default)
        => Task.FromResult(EmptyPeers);

    public Task<PeerRankingWeights> GetWeightsAsync(CancellationToken ct = default)
        => Task.FromResult(DefaultWeights);

    public Task UpdateWeightsAsync(PeerRankingWeights weights, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<PeerRankingStrategyState> GetStrategyAsync(CancellationToken ct = default)
        => Task.FromResult(DefaultStrategy);

    public Task UpdateStrategyAsync(string strategyName, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<PeerRankingHistoryEntry>> GetHistoryAsync(string nodeId, CancellationToken ct = default)
        => Task.FromResult(EmptyHistory);

    public Task<int> RecomputeAsync(CancellationToken ct = default)
        => Task.FromResult(0);
}
