using AutoFixture;
using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TestHelpers;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class PeerRankingViewModelTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();

    [Fact]
    public async Task LoadAsync_ShouldPopulateRankingWeightsStrategyAndPeers()
    {
        var service = new Mock<IPeerRankingManagementService>();
        service.Setup(x => x.GetRankingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<PeerRankingItem>)
            [
                new("peer-1", "Trusted", 88, 90, 84, 80, 70, 60, 50, 100, 2, 120, 97, new DateTime(2026, 5, 20), new DateTime(2026, 5, 29), 0),
                new("peer-2", "Standard", 55, 60, 52, 50, 45, 40, 30, 40, 3, 220, 80, new DateTime(2026, 5, 10), new DateTime(2026, 5, 29), 1)
            ]);
        service.Setup(x => x.GetWeightsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PeerRankingWeights(0.42, 0.18, 0.18, 0.08, 0.07, 0.07));
        service.Setup(x => x.GetStrategyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PeerRankingStrategyState("LeastLoaded", ["TopRanked", "LeastLoaded"]));
        service.Setup(x => x.GetHistoryAsync("peer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<PeerRankingHistoryEntry>)
            [
                new("peer-1", "bonus", 88.5, "Trusted", 0.5, "job_success", new DateTime(2026, 5, 29, 10, 0, 0))
            ]);

        var vm = new PeerRankingViewModel(service.Object, NullLogger<PeerRankingViewModel>.Instance);

        await vm.LoadAsync(CancellationToken.None);

        Assert.Equal(2, vm.FilteredPeers.Count);
        Assert.Equal(0.42, vm.SuccessRateWeight, 2);
        Assert.Equal("LeastLoaded", vm.SelectedStrategy);
        Assert.Equal("peer-1", vm.SelectedPeer?.NodeId);
    }

    [Fact]
    public async Task LoadHistoryAsync_ShouldPopulateHistoryForSelectedPeer()
    {
        var service = new Mock<IPeerRankingManagementService>();
        service.Setup(x => x.GetHistoryAsync("peer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<PeerRankingHistoryEntry>)
            [
                new("peer-1", "bonus", 88.5, "Trusted", 0.5, "job_success", new DateTime(2026, 5, 29, 10, 0, 0))
            ]);

        var vm = new PeerRankingViewModel(service.Object, NullLogger<PeerRankingViewModel>.Instance);

        await vm.LoadHistoryAsync("peer-1", CancellationToken.None);

        Assert.Single(vm.History);
        Assert.Equal("peer-1", vm.History[0].NodeId);
    }

    [Fact]
    public async Task SaveWeightsAsync_ShouldPersistEditedWeights()
    {
        PeerRankingWeights? capturedWeights = null;
        var service = new Mock<IPeerRankingManagementService>();
        service.Setup(x => x.UpdateWeightsAsync(It.IsAny<PeerRankingWeights>(), It.IsAny<CancellationToken>()))
            .Callback<PeerRankingWeights, CancellationToken>((w, _) => capturedWeights = w)
            .Returns(Task.CompletedTask);
        service.Setup(x => x.GetWeightsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PeerRankingWeights(0.42, 0.18, 0.18, 0.08, 0.07, 0.07));

        var vm = new PeerRankingViewModel(service.Object, NullLogger<PeerRankingViewModel>.Instance)
        {
            SuccessRateWeight = 0.10,
            LatencyWeight = 0.40,
            AvailabilityWeight = 0.20,
            TenureWeight = 0.10,
            CapacityWeight = 0.10,
            CatalogWeight = 0.10
        };

        await vm.SaveWeightsAsync(CancellationToken.None);

        Assert.NotNull(capturedWeights);
        Assert.Equal(0.10, capturedWeights!.SuccessRateWeight, 2);
        Assert.Equal("Pesos atualizados.", vm.StatusMessage);
    }

    [Fact]
    public async Task FilterText_ShouldReduceDisplayedPeers()
    {
        var service = new Mock<IPeerRankingManagementService>();
        service.Setup(x => x.GetRankingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<PeerRankingItem>)
            [
                new("peer-1", "Trusted", 88, 90, 84, 80, 70, 60, 50, 100, 2, 120, 97, new DateTime(2026, 5, 20), new DateTime(2026, 5, 29), 0),
                new("peer-2", "Standard", 55, 60, 52, 50, 45, 40, 30, 40, 3, 220, 80, new DateTime(2026, 5, 10), new DateTime(2026, 5, 29), 1)
            ]);
        service.Setup(x => x.GetWeightsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PeerRankingWeights(0.42, 0.18, 0.18, 0.08, 0.07, 0.07));
        service.Setup(x => x.GetStrategyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PeerRankingStrategyState("LeastLoaded", ["TopRanked", "LeastLoaded"]));
        service.Setup(x => x.GetHistoryAsync("peer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<PeerRankingHistoryEntry>)[]);

        var vm = new PeerRankingViewModel(service.Object, NullLogger<PeerRankingViewModel>.Instance);
        await vm.LoadAsync(CancellationToken.None);

        vm.FilterText = "peer-2";

        Assert.Single(vm.FilteredPeers);
        Assert.Equal("peer-2", vm.FilteredPeers[0].NodeId);
    }
}
