using Moq;
using System.Net.Http;

namespace KrnlAI.Desktop.Tests.Controls;

public class KnowledgeViewModelTests
{
    private readonly Mock<IKernelClient> _kernelMock;
    private readonly KnowledgeViewModel _vm;

    public KnowledgeViewModelTests()
    {
        _kernelMock = new Mock<IKernelClient>();
        _vm = new KnowledgeViewModel(_kernelMock.Object);
    }

    [Fact]
    public async Task SearchAsync_ValidQuery_ShouldPopulateResults()
    {
        var hits = new List<KnowledgeHit>
        {
            new("1", "Result A", 0.95, "doc1", DateTimeOffset.UtcNow),
            new("2", "Result B", 0.80, "doc2", DateTimeOffset.UtcNow),
        };
        _kernelMock.Setup(k => k.KnowledgeAskAsync("test", default))
            .ReturnsAsync(new KnowledgeQueryResult("test", hits, 2));

        _vm.Query = "test";
        await _vm.SearchAsync();

        Assert.Equal(2, _vm.Results.Count);
        Assert.Equal("Result A", _vm.Results[0].Content);
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ShouldNotCallApi()
    {
        _vm.Query = "";
        await _vm.SearchAsync();

        _kernelMock.Verify(k => k.KnowledgeAskAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task SearchAsync_ApiError_ShouldSetErrorMessage()
    {
        _kernelMock.Setup(k => k.KnowledgeAskAsync("fail", default))
            .ThrowsAsync(new HttpRequestException("network error"));

        _vm.Query = "fail";
        await _vm.SearchAsync();

        Assert.True(_vm.HasError);
        Assert.Contains("network error", _vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadStatsAsync_ShouldSetStats()
    {
        var stats = new KnowledgeStats(100, 5, 42, DateTimeOffset.UtcNow);
        _kernelMock.Setup(k => k.KnowledgeStatsAsync(default))
            .ReturnsAsync(stats);

        await _vm.LoadStatsAsync();

        Assert.NotNull(_vm.Stats);
        Assert.Equal(100, _vm.Stats.TotalEntries);
    }

    [Fact]
    public async Task LoadStatsAsync_ApiError_ShouldSetErrorMessage()
    {
        _kernelMock.Setup(k => k.KnowledgeStatsAsync(default))
            .ThrowsAsync(new InvalidOperationException("stats unavailable"));

        await _vm.LoadStatsAsync();

        Assert.True(_vm.HasError);
    }

    [Fact]
    public async Task LearnAsync_ShouldCallApi()
    {
        _kernelMock.Setup(k => k.KnowledgeLearnAsync("content", "source", null, default))
            .ReturnsAsync(new KnowledgeLearnResponse(true, "entry-1", null));

        var (success, _) = await _vm.LearnAsync("content", "source");

        Assert.True(success);
    }

    [Fact]
    public async Task LearnAsync_ApiError_ShouldReturnFalse()
    {
        _kernelMock.Setup(k => k.KnowledgeLearnAsync("content", "source", null, default))
            .ThrowsAsync(new HttpRequestException("fail"));

        var (success, error) = await _vm.LearnAsync("content", "source");

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public void ClearError_ShouldClearMessage()
    {
        _vm.ErrorMessage = "some error";
        _vm.ClearError();
        Assert.False(_vm.HasError);
    }

    [Fact]
    public async Task SearchAsync_ShouldToggleLoadingState()
    {
        _kernelMock.Setup(k => k.KnowledgeAskAsync("test", default))
            .ReturnsAsync(new KnowledgeQueryResult("test", [], 0));

        var loadingStates = new List<bool>();
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(KnowledgeViewModel.IsLoading))
                loadingStates.Add(_vm.IsLoading);
        };

        _vm.Query = "test";
        await _vm.SearchAsync();

        Assert.Equal(2, loadingStates.Count);
        Assert.True(loadingStates[0]);
        Assert.False(loadingStates[1]);
    }
}

