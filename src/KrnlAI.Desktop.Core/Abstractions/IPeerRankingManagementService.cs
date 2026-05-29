using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

/// <summary>
/// Manages the peer ranking dashboard used by the desktop application.
/// </summary>
public interface IPeerRankingManagementService
{
    Task<IReadOnlyList<PeerRankingItem>> GetRankingAsync(CancellationToken ct = default);
    Task<PeerRankingWeights> GetWeightsAsync(CancellationToken ct = default);
    Task UpdateWeightsAsync(PeerRankingWeights weights, CancellationToken ct = default);
    Task<PeerRankingStrategyState> GetStrategyAsync(CancellationToken ct = default);
    Task UpdateStrategyAsync(string strategyName, CancellationToken ct = default);
    Task<IReadOnlyList<PeerRankingHistoryEntry>> GetHistoryAsync(string nodeId, CancellationToken ct = default);
    Task<int> RecomputeAsync(CancellationToken ct = default);
}
