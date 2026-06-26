using Moq;

namespace KrnlAI.Desktop.Tests.ViewModels;

[Trait("Category", "Unit")]
public sealed class EpisodicMemoryViewModelTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeProperties()
    {
        var vm = new EpisodicMemoryViewModel();
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Empty(vm.Results);
        Assert.Empty(vm.SearchQuery);
        Assert.Null(vm.SelectedEpisode);
    }

    [Fact]
    public async Task SearchAsync_WhenQueryEmpty_ShouldNotCallApi()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = new EpisodicMemoryViewModel(kernelClient.Object);
        await vm.SearchAsync();
        kernelClient.Verify(k => k.SearchEpisodicMemoryAsync(It.IsAny<EpisodicMemorySearchRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchAsync_WhenQueryWhitespace_ShouldNotCallApi()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = new EpisodicMemoryViewModel(kernelClient.Object);
        vm.SearchQuery = "   ";
        await vm.SearchAsync();
        kernelClient.Verify(k => k.SearchEpisodicMemoryAsync(It.IsAny<EpisodicMemorySearchRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchAsync_ShouldCallApiAndUpdateResults()
    {
        var kernelClient = new Mock<IKernelClient>();
        var hits = new List<EpisodicMemoryHit>
        {
            new("ep-1", "Goal 1", "Summary 1", "completed", 0.95, DateTimeOffset.UtcNow),
            new("ep-2", "Goal 2", "Summary 2", "failed", 0.85, DateTimeOffset.UtcNow),
        };
        kernelClient.Setup(k => k.SearchEpisodicMemoryAsync(
            It.Is<EpisodicMemorySearchRequest>(r => r.Query == "test query"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EpisodicMemorySearchResult(hits, 2, "test query"));

        var vm = new EpisodicMemoryViewModel(kernelClient.Object);
        vm.SearchQuery = "test query";

        await vm.SearchAsync();

        Assert.Equal(2, vm.Results.Count);
        Assert.Equal("Summary 1", vm.Results[0].Summary);
        Assert.Equal("Summary 2", vm.Results[1].Summary);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task SearchAsync_WhenResultIsNull_ShouldClearResults()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.SearchEpisodicMemoryAsync(It.IsAny<EpisodicMemorySearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EpisodicMemorySearchResult?)null);

        var vm = new EpisodicMemoryViewModel(kernelClient.Object);
        vm.SearchQuery = "query";
        await vm.SearchAsync();

        Assert.Empty(vm.Results);
    }

    [Fact]
    public async Task SearchAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.SearchEpisodicMemoryAsync(It.IsAny<EpisodicMemorySearchRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("search error"));

        var vm = new EpisodicMemoryViewModel(kernelClient.Object);
        vm.SearchQuery = "query";

        await vm.SearchAsync();

        Assert.True(vm.HasError);
        Assert.Contains("search error", vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task SearchAsync_ShouldSetIsLoadingTrueDuringExecution()
    {
        var kernelClient = new Mock<IKernelClient>();
        var tcs = new TaskCompletionSource<EpisodicMemorySearchResult?>();
        kernelClient.Setup(k => k.SearchEpisodicMemoryAsync(It.IsAny<EpisodicMemorySearchRequest>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var vm = new EpisodicMemoryViewModel(kernelClient.Object);
        vm.SearchQuery = "query";

        var searchTask = vm.SearchAsync();
        Assert.True(vm.IsLoading);
        tcs.SetResult(new EpisodicMemorySearchResult([], 0, "query"));
        await searchTask;
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void SelectEpisode_ShouldSetSelectedEpisode()
    {
        var vm = new EpisodicMemoryViewModel();
        var hit = new EpisodicMemoryHit("ep-1", "Goal", "Summary", "completed", 0.9, DateTimeOffset.UtcNow);
        vm.SelectEpisodeCommand.Execute(hit);
        Assert.NotNull(vm.SelectedEpisode);
        Assert.Equal("ep-1", vm.SelectedEpisode!.EpisodeId);
    }

    [Fact]
    public void SearchCommand_ShouldExist()
    {
        var vm = new EpisodicMemoryViewModel();
        Assert.NotNull(vm.SearchCommand);
    }

    [Fact]
    public void ClearErrorCommand_ShouldClearError()
    {
        var vm = new EpisodicMemoryViewModel();
        vm.ErrorMessage = "error";
        vm.ClearErrorCommand.Execute(null);
        Assert.False(vm.HasError);
        Assert.Empty(vm.ErrorMessage);
    }
}
