using Moq;

namespace KrnlAI.Desktop.Tests.ViewModels;

[Trait("Category", "Unit")]
public sealed class SelfImprovementViewModelTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeProperties()
    {
        var vm = new SelfImprovementViewModel();
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Null(vm.Status);
    }

    [Fact]
    public async Task LoadStatusAsync_ShouldCallGetSelfImprovementStatusAsyncAndUpdate()
    {
        var kernelClient = new Mock<IKernelClient>();
        var status = new SelfImprovementStatus(true, true, DateTime.UtcNow, 10, 8, 2,
            [new CycleInfo("c1", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, "completed", "Cycle 1", 0.05)],
            [new TraceInfo("t1", "Cycle started", "info", "self-improvement", DateTime.UtcNow)]);
        kernelClient.Setup(k => k.GetSelfImprovementStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        var vm = new SelfImprovementViewModel(kernelClient.Object);

        await vm.LoadStatusAsync();

        Assert.NotNull(vm.Status);
        Assert.True(vm.Status.IsEnabled);
        Assert.Equal(10, vm.Status.TotalCycles);
        Assert.Single(vm.Status.RecentCycles);
        Assert.Single(vm.Status.RecentTraces);
    }

    [Fact]
    public async Task LoadStatusAsync_ShouldSetIsLoadingTrueDuringExecution()
    {
        var kernelClient = new Mock<IKernelClient>();
        var tcs = new TaskCompletionSource<SelfImprovementStatus?>();
        kernelClient.Setup(k => k.GetSelfImprovementStatusAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var vm = new SelfImprovementViewModel(kernelClient.Object);

        var task = vm.LoadStatusAsync();
        Assert.True(vm.IsLoading);
        tcs.SetResult(new SelfImprovementStatus(false, false, null, 0, 0, 0, [], []));
        await task;
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task LoadStatusAsync_WhenResultIsNull_ShouldClearStatus()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.GetSelfImprovementStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((SelfImprovementStatus?)null);

        var vm = new SelfImprovementViewModel(kernelClient.Object);

        await vm.LoadStatusAsync();

        Assert.Null(vm.Status);
        Assert.False(vm.HasError);
    }

    [Fact]
    public async Task LoadStatusAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.GetSelfImprovementStatusAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("status error"));

        var vm = new SelfImprovementViewModel(kernelClient.Object);

        await vm.LoadStatusAsync();

        Assert.True(vm.HasError);
        Assert.Contains("status error", vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void ClearError_ShouldClearErrorMessage()
    {
        var vm = new SelfImprovementViewModel();
        vm.ErrorMessage = "some error";
        Assert.True(vm.HasError);
        vm.ClearError();
        Assert.False(vm.HasError);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void ClearErrorCommand_ShouldClearErrorMessage()
    {
        var vm = new SelfImprovementViewModel();
        vm.ErrorMessage = "some error";
        vm.ClearErrorCommand.Execute(null);
        Assert.False(vm.HasError);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void LoadStatusCommand_ShouldExist()
    {
        var vm = new SelfImprovementViewModel();
        Assert.NotNull(vm.LoadStatusCommand);
    }
}
