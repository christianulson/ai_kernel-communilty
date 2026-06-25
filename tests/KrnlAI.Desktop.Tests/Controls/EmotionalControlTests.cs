using Moq;
using System.Net.Http;

namespace KrnlAI.Desktop.Tests.Controls;

public class EmotionalViewModelTests
{
    private readonly Mock<IKernelClient> _kernelMock;
    private readonly EmotionalViewModel _vm;

    public EmotionalViewModelTests()
    {
        _kernelMock = new Mock<IKernelClient>();
        _vm = new EmotionalViewModel(_kernelMock.Object);
    }

    [Fact]
    public async Task LoadCurrentStateAsync_ShouldSetState()
    {
        _kernelMock.Setup(k => k.GetEmotionalStateAsync("default", default))
            .ReturnsAsync(new EmotionalState(0.5, 0.3, 0.8, DateTimeOffset.UtcNow));

        await _vm.LoadCurrentStateAsync();

        Assert.NotNull(_vm.CurrentState);
        Assert.Equal(0.5, _vm.CurrentState.Valence);
        Assert.Equal(0.3, _vm.CurrentState.Arousal);
    }

    [Fact]
    public async Task LoadHistoryAsync_ShouldPopulateHistory()
    {
        var history = new List<EmotionalHistoryEntry>
        {
            new(DateTimeOffset.UtcNow.AddMinutes(-5), "User happy", 0.7, 0.4, "compliment"),
            new(DateTimeOffset.UtcNow, "User frustrated", -0.3, 0.6, "error"),
        };
        _kernelMock.Setup(k => k.EmotionalHistoryAsync(default))
            .ReturnsAsync(history);

        await _vm.LoadHistoryAsync();

        Assert.Equal(2, _vm.History.Count);
    }

    [Fact]
    public async Task LogEventAsync_ShouldCallApi()
    {
        _kernelMock.Setup(k => k.EmotionalEventAsync("praise", null, null, null, default))
            .ReturnsAsync(true);

        var success = await _vm.LogEventAsync("praise");

        Assert.True(success);
    }

    [Fact]
    public async Task LoadCurrentStateAsync_ApiError_ShouldSetError()
    {
        _kernelMock.Setup(k => k.GetEmotionalStateAsync("default", default))
            .ThrowsAsync(new HttpRequestException("emotional API down"));

        await _vm.LoadCurrentStateAsync();

        Assert.True(_vm.HasError);
    }

    [Fact]
    public async Task LogEventAsync_ApiError_ShouldReturnFalse()
    {
        _kernelMock.Setup(k => k.EmotionalEventAsync("fail", null, null, null, default))
            .ThrowsAsync(new HttpRequestException("fail"));

        var success = await _vm.LogEventAsync("fail");

        Assert.False(success);
    }

    [Fact]
    public async Task LoadHistoryAsync_EmptyList_ShouldBeEmpty()
    {
        _kernelMock.Setup(k => k.EmotionalHistoryAsync(default))
            .ReturnsAsync([]);

        await _vm.LoadHistoryAsync();

        Assert.Empty(_vm.History);
    }

    [Fact]
    public void ClearError_ShouldClear()
    {
        _vm.ErrorMessage = "err";
        _vm.ClearError();
        Assert.False(_vm.HasError);
    }
}

