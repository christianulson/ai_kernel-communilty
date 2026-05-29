using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using KrnlAI.Core.Abstractions.P2P.Ranking;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using DesktopPeerRankingHistoryEntry = KrnlAI.Desktop.Core.Models.PeerRankingHistoryEntry;

namespace KrnlAI.Desktop.App.Services;

/// <summary>
/// HTTP-backed peer ranking dashboard service.
/// </summary>
public sealed class HttpPeerRankingManagementService(HttpClient httpClient) : IPeerRankingManagementService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<PeerRankingItem>> GetRankingAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("/p2p/ranking", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<RankingResponse>(JsonOptions, ct).ConfigureAwait(false);
        return payload?.Ranking?.Select(MapRankingItem).ToList() ?? [];
    }

    public async Task<PeerRankingWeights> GetWeightsAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("/p2p/ranking/weights", ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return new PeerRankingWeights(0.35, 0.20, 0.20, 0.05, 0.10, 0.10);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PeerRankingWeights>(JsonOptions, ct).ConfigureAwait(false)
            ?? new PeerRankingWeights(0.35, 0.20, 0.20, 0.05, 0.10, 0.10);
    }

    public async Task UpdateWeightsAsync(PeerRankingWeights weights, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync("/p2p/ranking/weights", weights, JsonOptions, ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return;

        response.EnsureSuccessStatusCode();
    }

    public async Task<PeerRankingStrategyState> GetStrategyAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("/p2p/ranking/strategy", ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return new PeerRankingStrategyState("TopRanked", ["TopRanked"]);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PeerRankingStrategyState>(JsonOptions, ct).ConfigureAwait(false)
            ?? new PeerRankingStrategyState("TopRanked", ["TopRanked"]);
    }

    public async Task UpdateStrategyAsync(string strategyName, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync("/p2p/ranking/strategy", new RankingStrategyUpdateRequest(strategyName), JsonOptions, ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return;

        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<DesktopPeerRankingHistoryEntry>> GetHistoryAsync(string nodeId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/p2p/ranking/history/{nodeId}", ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return [];

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<DesktopPeerRankingHistoryEntry>>(JsonOptions, ct).ConfigureAwait(false)
            ?? [];
    }

    public async Task<int> RecomputeAsync(CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync("/p2p/ranking/recompute", content: null, ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return 0;

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>(JsonOptions, ct).ConfigureAwait(false);
        return payload?.GetValueOrDefault("updated") ?? 0;
    }

    private static PeerRankingItem MapRankingItem(PeerScorePayload payload)
        => new(
            payload.NodeId,
            ToTier(payload.OverallScore),
            payload.OverallScore,
            payload.SuccessRateScore,
            payload.LatencyScore,
            payload.AvailabilityScore,
            payload.TenureScore,
            payload.CapacityScore,
            payload.CatalogScore,
            payload.TotalJobsExecuted,
            payload.TotalJobsFailed,
            payload.AvgResponseTimeMs,
            payload.UptimePercentage,
            payload.FirstSeen,
            payload.LastSeen,
            payload.QuarantineCount);

    private static string ToTier(double score)
        => score >= 91 ? "Preferred"
        : score >= 71 ? "Trusted"
        : score >= 31 ? "Standard"
        : "Untrusted";

    private sealed record RankingResponse(IReadOnlyList<PeerScorePayload>? Ranking, bool Enabled, int Count);

    private sealed record PeerScorePayload(
        string NodeId,
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
}
