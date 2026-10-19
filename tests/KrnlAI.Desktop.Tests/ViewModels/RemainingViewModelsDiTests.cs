using AutoFixture;
using Moq;
using TestHelpers;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class MemoryViewModelDiTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();

    private MemoryViewModel CreateVm(Mock<IKernelClient>? kc = null)
        => new(kc?.Object ?? Mock.Of<IKernelClient>());

    [Fact]
    public void DefaultTab_ShouldBeSearch()
    {
        var vm = CreateVm();
        Assert.Equal("search", vm.MemoryTab);
    }

    [Fact]
    public async Task SearchMemory_WithQuery_ShouldReturnResults()
    {
        var kc = new Mock<IKernelClient>();
        kc.Setup(x => x.SearchMemoryAsync("test", 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemorySearchResult([new("h1", "content", "web", 0.95, DateTime.UtcNow, null)], 1, 0.5));

        var vm = CreateVm(kc);
        vm.MemoryQuery = "test";
        vm.SearchMemoryCommand.Execute(null);
        await Task.Delay(100);

        Assert.NotEmpty(vm.MemoryResults);
    }

    [Fact]
    public void SetTabs_ShouldSwitch()
    {
        var vm = CreateVm();
        vm.SetTabMetricsCommand.Execute(null);
        Assert.Equal("metrics", vm.MemoryTab);
        vm.SetTabWorkingCommand.Execute(null);
        Assert.Equal("working", vm.MemoryTab);
        vm.SetTabSearchCommand.Execute(null);
        Assert.Equal("search", vm.MemoryTab);
    }
}

public sealed class PoliciesViewModelDiTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();

    [Fact]
    public void PolicyDomains_ShouldContainDefault()
    {
        Assert.Contains("general", new PoliciesViewModel(Mock.Of<IKernelClient>()).PolicyDomains);
    }
}

public sealed class EpisodesViewModelDiTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();

    [Fact]
    public void IsLoading_Default_ShouldBeFalse()
    {
        Assert.False(new EpisodesViewModel(Mock.Of<IKernelClient>()).IsLoading);
    }

    [Fact]
    public async Task LoadEpisodes_ShouldReturnList()
    {
        var kc = new Mock<IKernelClient>();
        kc.Setup(x => x.SearchEpisodesAsync(It.IsAny<EpisodeSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EpisodeSearchResult([new("e1", "g1", "completed", DateTime.UtcNow, DateTime.UtcNow, 1000, "ok", 1.0)], 1, 1, 20));

        var vm = new EpisodesViewModel(kc.Object);
        vm.LoadEpisodesCommand.Execute(null);
        await Task.Delay(100);

        kc.Verify(x => x.SearchEpisodesAsync(It.IsAny<EpisodeSearchRequest>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}

public sealed class CausalGraphViewModelDiTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();

    [Fact]
    public void DefaultTab_ShouldBeQuery()
    {
        Assert.Equal("query", new CausalGraphViewModel(Mock.Of<IKernelClient>()).Tab);
    }

    [Fact]
    public void SetTabs_ShouldSwitch()
    {
        var vm = new CausalGraphViewModel(Mock.Of<IKernelClient>());
        vm.SetPredictTabCommand.Execute(null);
        Assert.Equal("predict", vm.Tab);
        vm.SetQueryTabCommand.Execute(null);
        Assert.Equal("query", vm.Tab);
    }

    [Fact]
    public async Task Search_WithQuery_ShouldReturnResult()
    {
        var kc = new Mock<IKernelClient>();
        kc.Setup(x => x.GetCausalQueryAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CausalQueryResult("test", [], []));

        var vm = new CausalGraphViewModel(kc.Object);
        vm.Query = "test";
        vm.SearchCommand.Execute(null);
        await Task.Delay(100);

        kc.Verify(x => x.GetCausalQueryAsync("test", It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Predict_WithAction_ShouldReturnResult()
    {
        var kc = new Mock<IKernelClient>();
        kc.Setup(x => x.GetCausalPredictionAsync("action", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CausalPrediction("action", "outcome", 0.85, []));

        var vm = new CausalGraphViewModel(kc.Object);
        vm.PredictAction = "action";
        vm.PredictCommand.Execute(null);
        await Task.Delay(100);

        kc.Verify(x => x.GetCausalPredictionAsync("action", It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}

public sealed class ProfileViewModelDiTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();

    [Fact]
    public void DefaultState_ShouldBeCorrect()
    {
        var vm = new ProfileViewModel(Mock.Of<IKernelClient>());
        Assert.False(vm.IsLoading);
        Assert.False(vm.IsSaving);
    }
}

public sealed class DocumentViewModelDiTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();

    [Fact]
    public void IsLoading_Default_ShouldBeFalse()
    {
        Assert.False(new DocumentViewModel(Mock.Of<IKernelClient>()).IsLoading);
    }
}

public sealed class ArchiveViewModelDiTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();

    [Fact]
    public void Stats_Default_ShouldBeNull()
    {
        Assert.Null(new ArchiveViewModel(Mock.Of<IKernelClient>()).Stats);
    }
}

public sealed class ModelRegistryViewModelDiTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();

    [Fact]
    public void Models_Default_ShouldBeEmpty()
    {
        Assert.Empty(new ModelRegistryViewModel(Mock.Of<IKernelClient>()).Models);
    }
}

public sealed class VersionsViewModelDiTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();

    [Fact]
    public void Contracts_Default_ShouldBeEmpty()
    {
        Assert.NotNull(new VersionsViewModel(Mock.Of<IKernelClient>()).Contracts);
    }
}

public sealed class SessionsViewModelDiTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();

    [Fact]
    public void Shares_Default_ShouldBeEmpty()
    {
        Assert.Empty(new SessionsViewModel(Mock.Of<IKernelClient>()).Shares);
    }
}

public sealed class VideoCallViewModelDiTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();

    [Fact]
    public void DefaultState_ShouldBeIdle()
    {
        var vm = new VideoCallViewModel(Mock.Of<Core.Services.IWebRtcService>());
        Assert.False(vm.IsInVideoCall);
        Assert.Equal("Idle", vm.VideoCallState);
    }
}

public sealed class BenchmarkViewModelDiTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();

    [Fact]
    public void IsLoading_Default_ShouldBeFalse()
    {
        Assert.False(new BenchmarkViewModel(Mock.Of<IKernelClient>()).IsLoading);
    }
}
