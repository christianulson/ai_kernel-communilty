using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class PeerRankingViewModelTests
{
    [Fact]
    public async Task LoadAsync_ShouldPopulateRankingWeightsStrategyAndPeers()
    {
        var service = new FakePeerRankingManagementService();
        var vm = new PeerRankingViewModel(service, NullLogger<PeerRankingViewModel>.Instance);

        await vm.LoadAsync(CancellationToken.None);

        Assert.Equal(2, vm.FilteredPeers.Count);
        Assert.Equal(0.42, vm.SuccessRateWeight, 2);
        Assert.Equal("LeastLoaded", vm.SelectedStrategy);
        Assert.Equal("peer-1", vm.SelectedPeer?.NodeId);
    }

    [Fact]
    public async Task LoadHistoryAsync_ShouldPopulateHistoryForSelectedPeer()
    {
        var service = new FakePeerRankingManagementService();
        var vm = new PeerRankingViewModel(service, NullLogger<PeerRankingViewModel>.Instance);

        await vm.LoadHistoryAsync("peer-1", CancellationToken.None);

        Assert.Single(vm.History);
        Assert.Equal("peer-1", vm.History[0].NodeId);
    }

    [Fact]
    public async Task SaveWeightsAsync_ShouldPersistEditedWeights()
    {
        var service = new FakePeerRankingManagementService();
        var vm = new PeerRankingViewModel(service, NullLogger<PeerRankingViewModel>.Instance)
        {
            SuccessRateWeight = 0.10,
            LatencyWeight = 0.40,
            AvailabilityWeight = 0.20,
            TenureWeight = 0.10,
            CapacityWeight = 0.10,
            CatalogWeight = 0.10
        };

        await vm.SaveWeightsAsync(CancellationToken.None);

        Assert.Equal(0.10, service.LastWeights!.SuccessRateWeight, 2);
        Assert.Equal("Pesos atualizados.", vm.StatusMessage);
    }

    [Fact]
    public async Task FilterText_ShouldReduceDisplayedPeers()
    {
        var service = new FakePeerRankingManagementService();
        var vm = new PeerRankingViewModel(service, NullLogger<PeerRankingViewModel>.Instance);
        await vm.LoadAsync(CancellationToken.None);

        vm.FilterText = "peer-2";

        Assert.Single(vm.FilteredPeers);
        Assert.Equal("peer-2", vm.FilteredPeers[0].NodeId);
    }

    private sealed class FakePeerRankingManagementService : IPeerRankingManagementService
    {
        public PeerRankingWeights? LastWeights { get; private set; }
        public string LastStrategy { get; private set; } = "TopRanked";

        private static readonly IReadOnlyList<PeerRankingItem> Ranking =
        [
            new PeerRankingItem("peer-1", "Trusted", 88, 90, 84, 80, 70, 60, 50, 100, 2, 120, 97, new DateTime(2026, 5, 20), new DateTime(2026, 5, 29), 0),
            new PeerRankingItem("peer-2", "Standard", 55, 60, 52, 50, 45, 40, 30, 40, 3, 220, 80, new DateTime(2026, 5, 10), new DateTime(2026, 5, 29), 1)
        ];

        private static readonly IReadOnlyList<PeerRankingHistoryEntry> History =
        [
            new PeerRankingHistoryEntry("peer-1", "bonus", 88.5, "Trusted", 0.5, "job_success", new DateTime(2026, 5, 29, 10, 0, 0))
        ];

        public Task<IReadOnlyList<PeerRankingItem>> GetRankingAsync(CancellationToken ct = default)
            => Task.FromResult(Ranking);

        public Task<PeerRankingWeights> GetWeightsAsync(CancellationToken ct = default)
            => Task.FromResult(new PeerRankingWeights(0.42, 0.18, 0.18, 0.08, 0.07, 0.07));

        public Task UpdateWeightsAsync(PeerRankingWeights weights, CancellationToken ct = default)
        {
            LastWeights = weights;
            return Task.CompletedTask;
        }

        public Task<PeerRankingStrategyState> GetStrategyAsync(CancellationToken ct = default)
            => Task.FromResult(new PeerRankingStrategyState("LeastLoaded", ["TopRanked", "LeastLoaded"]));

        public Task UpdateStrategyAsync(string strategyName, CancellationToken ct = default)
        {
            LastStrategy = strategyName;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PeerRankingHistoryEntry>> GetHistoryAsync(string nodeId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PeerRankingHistoryEntry>>(History.Where(entry => entry.NodeId == nodeId).ToList());

        public Task<int> RecomputeAsync(CancellationToken ct = default)
            => Task.FromResult(2);
    }
}
