using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Moq;

namespace KrnlAI.Desktop.Tests.ViewModels;

[Trait("Category", "Unit")]
public sealed class EmotionalViewModelTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeProperties()
    {
        var vm = new EmotionalViewModel();
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Null(vm.CurrentState);
        Assert.Empty(vm.History);
    }

    [Fact]
    public void ValenceLabel_WhenCurrentStateIsNull_ShouldReturnDash()
    {
        var vm = new EmotionalViewModel();
        Assert.Equal("—", vm.ValenceLabel);
    }

    [Fact]
    public void ArousalLabel_WhenCurrentStateIsNull_ShouldReturnDash()
    {
        var vm = new EmotionalViewModel();
        Assert.Equal("—", vm.ArousalLabel);
    }

    [Fact]
    public void MoodIcon_WhenCurrentStateIsNull_ShouldReturnDefaultIcon()
    {
        var vm = new EmotionalViewModel();
        Assert.Equal("🧐", vm.MoodIcon);
    }

    [Fact]
    public void ValenceLabel_WhenPositive_ShouldReturnPositivo()
    {
        var vm = new EmotionalViewModel(Mock.Of<IKernelClient>(), "user1");
        vm.CurrentState = new EmotionalState(0.5, 0.2, 0.7, DateTimeOffset.UtcNow);
        Assert.Equal("Positivo", vm.ValenceLabel);
    }

    [Fact]
    public void ValenceLabel_WhenNegative_ShouldReturnNegativo()
    {
        var vm = new EmotionalViewModel(Mock.Of<IKernelClient>(), "user1");
        vm.CurrentState = new EmotionalState(-0.5, 0.2, 0.3, DateTimeOffset.UtcNow);
        Assert.Equal("Negativo", vm.ValenceLabel);
    }

    [Fact]
    public void ArousalLabel_WhenHigh_ShouldReturnAlto()
    {
        var vm = new EmotionalViewModel(Mock.Of<IKernelClient>(), "user1");
        vm.CurrentState = new EmotionalState(0.1, 0.7, 0.5, DateTimeOffset.UtcNow);
        Assert.Equal("Alto", vm.ArousalLabel);
    }

    [Fact]
    public void ArousalLabel_WhenLow_ShouldReturnBaixo()
    {
        var vm = new EmotionalViewModel(Mock.Of<IKernelClient>(), "user1");
        vm.CurrentState = new EmotionalState(0.1, 0.2, 0.5, DateTimeOffset.UtcNow);
        Assert.Equal("Baixo", vm.ArousalLabel);
    }

    [Fact]
    public async Task LoadCurrentStateAsync_ShouldCallGetEmotionalStateAsync()
    {
        var kernelClient = new Mock<IKernelClient>();
        var state = new EmotionalState(0.3, 0.6, 0.5, DateTimeOffset.UtcNow);
        kernelClient.Setup(k => k.GetEmotionalStateAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(state);

        var vm = new EmotionalViewModel(kernelClient.Object, "user1");
        await vm.LoadCurrentStateAsync();

        Assert.NotNull(vm.CurrentState);
        Assert.Equal(0.3, vm.CurrentState.Valence);
        Assert.Equal(0.6, vm.CurrentState.Arousal);
    }

    [Fact]
    public async Task LoadCurrentStateAsync_ShouldManageLoadingState()
    {
        var kernelClient = new Mock<IKernelClient>();
        var tcs = new TaskCompletionSource<EmotionalState?>();
        kernelClient.Setup(k => k.GetEmotionalStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var vm = new EmotionalViewModel(kernelClient.Object, "user1");
        var task = vm.LoadCurrentStateAsync();
        Assert.True(vm.IsLoading);
        tcs.SetResult(new EmotionalState(0.3, 0.6, 0.5, DateTimeOffset.UtcNow));
        await task;
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task LoadCurrentStateAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.GetEmotionalStateAsync("user1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("emotional error"));

        var vm = new EmotionalViewModel(kernelClient.Object, "user1");
        await vm.LoadCurrentStateAsync();

        Assert.True(vm.HasError);
        Assert.Contains("emotional error", vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task LoadHistoryAsync_ShouldCallEmotionalHistoryAsyncAndUpdateHistory()
    {
        var kernelClient = new Mock<IKernelClient>();
        var entries = new List<EmotionalHistoryEntry>
        {
            new(DateTimeOffset.UtcNow, "event1", 0.5, 0.3, "trigger1"),
            new(DateTimeOffset.UtcNow, "event2", -0.2, 0.7, "trigger2"),
        };
        kernelClient.Setup(k => k.EmotionalHistoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        var vm = new EmotionalViewModel(kernelClient.Object, "user1");
        await vm.LoadHistoryAsync();

        Assert.Equal(2, vm.History.Count);
        Assert.Equal("event1", vm.History[0].Event);
        Assert.Equal("event2", vm.History[1].Event);
    }

    [Fact]
    public async Task LoadHistoryAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.EmotionalHistoryAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("history error"));

        var vm = new EmotionalViewModel(kernelClient.Object, "user1");
        await vm.LoadHistoryAsync();

        Assert.True(vm.HasError);
        Assert.Contains("history error", vm.ErrorMessage);
    }

    [Fact]
    public async Task LogEventAsync_ShouldCallEmotionalEventAsync()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.EmotionalEventAsync("joy", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var vm = new EmotionalViewModel(kernelClient.Object, "user1");
        var result = await vm.LogEventAsync("joy");

        Assert.True(result);
        kernelClient.Verify(k => k.EmotionalEventAsync("joy", null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogEventAsync_WhenApiThrows_ShouldReturnFalse()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.EmotionalEventAsync("sad", null, null, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("log error"));

        var vm = new EmotionalViewModel(kernelClient.Object, "user1");
        var result = await vm.LogEventAsync("sad");

        Assert.False(result);
    }

    [Fact]
    public void ClearError_ShouldClearErrorMessage()
    {
        var vm = new EmotionalViewModel();
        vm.ErrorMessage = "some error";

        vm.ClearError();

        Assert.False(vm.HasError);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void ClearErrorCommand_ShouldClearErrorMessage()
    {
        var vm = new EmotionalViewModel();
        vm.ErrorMessage = "some error";

        vm.ClearErrorCommand.Execute(null);

        Assert.False(vm.HasError);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void LoadCurrentStateCommand_ShouldExist()
    {
        var vm = new EmotionalViewModel();
        Assert.NotNull(vm.LoadCurrentStateCommand);
    }

    [Fact]
    public void LoadHistoryCommand_ShouldExist()
    {
        var vm = new EmotionalViewModel();
        Assert.NotNull(vm.LoadHistoryCommand);
    }

    [Fact]
    public void MoodIcon_WhenPositiveHighArousal_ShouldReturnLightning()
    {
        var vm = new EmotionalViewModel(Mock.Of<IKernelClient>(), "user1");
        vm.CurrentState = new EmotionalState(0.5, 0.7, 0.8, DateTimeOffset.UtcNow);
        Assert.Equal("⚡", vm.MoodIcon);
    }

    [Fact]
    public void MoodIcon_WhenPositiveLowArousal_ShouldReturnSmile()
    {
        var vm = new EmotionalViewModel(Mock.Of<IKernelClient>(), "user1");
        vm.CurrentState = new EmotionalState(0.5, 0.2, 0.4, DateTimeOffset.UtcNow);
        Assert.Equal("😊", vm.MoodIcon);
    }

    [Fact]
    public void MoodIcon_WhenNegativeHighArousal_ShouldReturnAnxious()
    {
        var vm = new EmotionalViewModel(Mock.Of<IKernelClient>(), "user1");
        vm.CurrentState = new EmotionalState(-0.5, 0.7, 0.3, DateTimeOffset.UtcNow);
        Assert.Equal("😰", vm.MoodIcon);
    }

    [Fact]
    public void MoodIcon_WhenNegativeLowArousal_ShouldReturnSad()
    {
        var vm = new EmotionalViewModel(Mock.Of<IKernelClient>(), "user1");
        vm.CurrentState = new EmotionalState(-0.5, 0.2, 0.2, DateTimeOffset.UtcNow);
        Assert.Equal("😔", vm.MoodIcon);
    }
}
