using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Moq;

namespace KrnlAI.Desktop.Tests.ViewModels;

[Trait("Category", "Unit")]
public sealed class FeedbackViewModelTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeProperties()
    {
        var vm = new FeedbackViewModel();
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Empty(vm.History);
        Assert.Null(vm.Average);
        Assert.Equal(5, vm.Rating);
    }

    [Fact]
    public async Task LoadHistoryAsync_ShouldCallApiAndUpdateHistory()
    {
        var kernelClient = new Mock<IKernelClient>();
        var history = new List<FeedbackHistoryEntry>
        {
            new("fb-1", "ep-1", 5, "Great!", "general", DateTimeOffset.UtcNow.AddHours(-1)),
            new("fb-2", "ep-2", 4, "Good", "bug", DateTimeOffset.UtcNow.AddMinutes(-30)),
        };
        var avg = new FeedbackAverage(2, 4.5, 0, 0, 0, 1, 1);
        kernelClient.Setup(k => k.GetFeedbackHistoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);
        kernelClient.Setup(k => k.GetFeedbackAverageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(avg);

        var vm = new FeedbackViewModel(kernelClient.Object);
        await vm.LoadHistoryAsync().ConfigureAwait(false);

        Assert.Equal(2, vm.History.Count);
        Assert.Equal("Great!", vm.History[0].Comment);
        Assert.NotNull(vm.Average);
        Assert.Equal(4.5, vm.Average!.AverageRating);
    }

    [Fact]
    public async Task LoadHistoryAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.GetFeedbackHistoryAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("history error"));

        var vm = new FeedbackViewModel(kernelClient.Object);
        await vm.LoadHistoryAsync().ConfigureAwait(false);

        Assert.True(vm.HasError);
        Assert.Contains("history error", vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadHistoryAsync_ShouldSetIsLoadingTrueDuringExecution()
    {
        var kernelClient = new Mock<IKernelClient>();
        var tcs = new TaskCompletionSource<List<FeedbackHistoryEntry>>();
        kernelClient.Setup(k => k.GetFeedbackHistoryAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);
        kernelClient.Setup(k => k.GetFeedbackAverageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeedbackAverage?)null);

        var vm = new FeedbackViewModel(kernelClient.Object);
        var loadTask = vm.LoadHistoryAsync();
        Assert.True(vm.IsLoading);
        tcs.SetResult([]);
        await loadTask.ConfigureAwait(false);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task SubmitAsync_WithValidInput_ShouldCallApiAndClearForm()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.SubmitFeedbackAsync(It.IsAny<FeedbackRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeedbackResponse(true, "fb-new", "Feedback submitted"));

        var vm = new FeedbackViewModel(kernelClient.Object);
        vm.Rating = 4;
        vm.Comment = "Nice work";
        vm.Category = "general";

        var result = await vm.SubmitAsync().ConfigureAwait(false);

        Assert.True(result);
        Assert.Equal(5, vm.Rating);
        Assert.Empty(vm.Comment);
        Assert.Empty(vm.Category);
        kernelClient.Verify(k => k.SubmitFeedbackAsync(
            It.Is<FeedbackRequest>(r => r.Rating == 4 && r.Comment == "Nice work" && r.Category == "general"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.SubmitFeedbackAsync(It.IsAny<FeedbackRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("submit error"));

        var vm = new FeedbackViewModel(kernelClient.Object);
        vm.Rating = 3;

        var result = await vm.SubmitAsync().ConfigureAwait(false);

        Assert.False(result);
        Assert.True(vm.HasError);
        Assert.Contains("submit error", vm.ErrorMessage);
    }

    [Fact]
    public void ClearErrorCommand_ShouldClearErrorMessage()
    {
        var vm = new FeedbackViewModel();
        vm.ErrorMessage = "error";
        vm.ClearErrorCommand.Execute(null);
        Assert.False(vm.HasError);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void LoadHistoryCommand_ShouldExist()
    {
        var vm = new FeedbackViewModel();
        Assert.NotNull(vm.LoadHistoryCommand);
    }

    [Fact]
    public void SubmitCommand_ShouldExist()
    {
        var vm = new FeedbackViewModel();
        Assert.NotNull(vm.SubmitCommand);
    }
}
