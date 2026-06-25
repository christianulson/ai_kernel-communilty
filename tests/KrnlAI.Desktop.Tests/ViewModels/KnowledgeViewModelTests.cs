using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Moq;

namespace KrnlAI.Desktop.Tests.ViewModels;

[Trait("Category", "Unit")]
public sealed class KnowledgeViewModelTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeProperties()
    {
        var vm = new KnowledgeViewModel();
        Assert.False(vm.IsLoading);
        Assert.False(vm.IsLearning);
        Assert.False(vm.HasError);
        Assert.Empty(vm.Query);
        Assert.Empty(vm.Results);
        Assert.Null(vm.Stats);
    }

    [Fact]
    public async Task SearchAsync_WhenQueryEmpty_ShouldNotCallApi()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = new KnowledgeViewModel(kernelClient.Object);

        await vm.SearchAsync().ConfigureAwait(false);

        kernelClient.Verify(k => k.KnowledgeAskAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchAsync_WhenQueryWhitespace_ShouldNotCallApi()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = new KnowledgeViewModel(kernelClient.Object);
        vm.Query = "   ";

        await vm.SearchAsync().ConfigureAwait(false);

        kernelClient.Verify(k => k.KnowledgeAskAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchAsync_ShouldCallKnowledgeAskAsyncAndUpdateResults()
    {
        var kernelClient = new Mock<IKernelClient>();
        var hits = new List<KnowledgeHit>
        {
            new("id1", "content1", 0.95, "src1", DateTimeOffset.UtcNow),
            new("id2", "content2", 0.85, "src2", DateTimeOffset.UtcNow),
        };
        kernelClient.Setup(k => k.KnowledgeAskAsync("test query", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeQueryResult("test query", hits, 2));

        var vm = new KnowledgeViewModel(kernelClient.Object);
        vm.Query = "test query";

        await vm.SearchAsync().ConfigureAwait(false);

        Assert.Equal(2, vm.Results.Count);
        Assert.Equal("content1", vm.Results[0].Content);
        Assert.Equal("content2", vm.Results[1].Content);
    }

    [Fact]
    public async Task SearchAsync_ShouldSetIsLoadingTrueDuringExecution()
    {
        var kernelClient = new Mock<IKernelClient>();
        var tcs = new TaskCompletionSource<KnowledgeQueryResult?>();
        kernelClient.Setup(k => k.KnowledgeAskAsync("query", It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var vm = new KnowledgeViewModel(kernelClient.Object);
        vm.Query = "query";

        var searchTask = vm.SearchAsync();
        Assert.True(vm.IsLoading);
        tcs.SetResult(new KnowledgeQueryResult("query", [], 0));
        await searchTask.ConfigureAwait(false);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task SearchAsync_WhenResultIsNull_ShouldClearResults()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.KnowledgeAskAsync("query", It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeQueryResult?)null);

        var vm = new KnowledgeViewModel(kernelClient.Object);
        vm.Query = "query";

        await vm.SearchAsync().ConfigureAwait(false);

        Assert.Empty(vm.Results);
        Assert.False(vm.HasError);
    }

    [Fact]
    public async Task SearchAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.KnowledgeAskAsync("query", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("network error"));

        var vm = new KnowledgeViewModel(kernelClient.Object);
        vm.Query = "query";

        await vm.SearchAsync().ConfigureAwait(false);

        Assert.True(vm.HasError);
        Assert.Contains("network error", vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task StatsAsync_ShouldCallKnowledgeStatsAsyncAndUpdateStats()
    {
        var kernelClient = new Mock<IKernelClient>();
        var stats = new KnowledgeStats(100, 5, 20, DateTimeOffset.UtcNow);
        kernelClient.Setup(k => k.KnowledgeStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var vm = new KnowledgeViewModel(kernelClient.Object);

        await vm.LoadStatsAsync().ConfigureAwait(false);

        Assert.Equal(100, vm.Stats?.TotalEntries);
        Assert.Equal(5, vm.Stats?.TotalSources);
    }

    [Fact]
    public async Task StatsAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.KnowledgeStatsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("stats error"));

        var vm = new KnowledgeViewModel(kernelClient.Object);

        await vm.LoadStatsAsync().ConfigureAwait(false);

        Assert.True(vm.HasError);
        Assert.Contains("stats error", vm.ErrorMessage);
    }

    [Fact]
    public void ClearError_ShouldClearErrorMessage()
    {
        var vm = new KnowledgeViewModel();
        vm.ErrorMessage = "some error";
        Assert.True(vm.HasError);

        vm.ClearError();

        Assert.False(vm.HasError);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void ClearErrorCommand_ShouldClearErrorMessage()
    {
        var vm = new KnowledgeViewModel();
        vm.ErrorMessage = "some error";

        vm.ClearErrorCommand.Execute(null);

        Assert.False(vm.HasError);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void SearchCommand_ShouldExist()
    {
        var vm = new KnowledgeViewModel();
        Assert.NotNull(vm.SearchCommand);
    }

    [Fact]
    public void LoadStatsCommand_ShouldExist()
    {
        var vm = new KnowledgeViewModel();
        Assert.NotNull(vm.LoadStatsCommand);
    }
}
