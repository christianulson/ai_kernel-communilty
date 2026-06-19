using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Moq;

namespace KrnlAI.Desktop.Tests.ViewModels;

[Trait("Category", "Unit")]
public sealed class PlanViewModelTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeProperties()
    {
        var vm = new PlanViewModel();
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Empty(vm.Steps);
        Assert.Null(vm.CurrentPlan);
    }

    [Fact]
    public async Task LoadPlanAsync_ShouldCallGetCurrentPlanAndUpdateProperties()
    {
        var kernelClient = new Mock<IKernelClient>();
        var steps = new List<PlanStep>
        {
            new(0, "Step 1", "Detail 1", PlanStepStatus.Completed, DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow.AddMinutes(-8), "Ok"),
            new(1, "Step 2", "Detail 2", PlanStepStatus.InProgress, DateTime.UtcNow.AddMinutes(-5), null, null),
        };
        var plan = new PlanInfo("plan-1", "Test goal", "Test description", PlanStatus.InProgress, 0.5, 4, 2, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
        var result = new PlanExecutionResult("plan-1", true, plan, steps, null);
        kernelClient.Setup(k => k.GetCurrentPlanAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var vm = new PlanViewModel(kernelClient.Object);
        await vm.LoadPlanAsync();

        Assert.NotNull(vm.CurrentPlan);
        Assert.Equal("plan-1", vm.CurrentPlan!.Id);
        Assert.Equal(2, vm.Steps.Count);
        Assert.Equal("Step 1", vm.Steps[0].Description);
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
    }

    [Fact]
    public async Task LoadPlanAsync_WhenResultIsNull_ShouldClearState()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.GetCurrentPlanAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlanExecutionResult?)null);

        var vm = new PlanViewModel(kernelClient.Object);
        await vm.LoadPlanAsync();

        Assert.Null(vm.CurrentPlan);
        Assert.Empty(vm.Steps);
    }

    [Fact]
    public async Task LoadPlanAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.GetCurrentPlanAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("plan error"));

        var vm = new PlanViewModel(kernelClient.Object);
        await vm.LoadPlanAsync();

        Assert.True(vm.HasError);
        Assert.Contains("plan error", vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task LoadPlanAsync_ShouldSetIsLoadingTrueDuringExecution()
    {
        var kernelClient = new Mock<IKernelClient>();
        var tcs = new TaskCompletionSource<PlanExecutionResult?>();
        kernelClient.Setup(k => k.GetCurrentPlanAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var vm = new PlanViewModel(kernelClient.Object);
        var loadTask = vm.LoadPlanAsync();
        Assert.True(vm.IsLoading);
        tcs.SetResult(new PlanExecutionResult("p1", false, null, [], null));
        await loadTask;
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void LoadPlanCommand_ShouldExist()
    {
        var vm = new PlanViewModel();
        Assert.NotNull(vm.LoadPlanCommand);
    }

    [Fact]
    public void ClearErrorCommand_ShouldClearErrorMessage()
    {
        var vm = new PlanViewModel();
        vm.ErrorMessage = "some error";
        Assert.True(vm.HasError);
        vm.ClearErrorCommand.Execute(null);
        Assert.False(vm.HasError);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void Progress_ShouldReturnZeroWhenNoPlan()
    {
        var vm = new PlanViewModel();
        Assert.Equal(0.0, vm.Progress);
    }

    [Fact]
    public async Task Progress_ShouldReturnPlanProgress()
    {
        var kernelClient = new Mock<IKernelClient>();
        var steps = new List<PlanStep>
        {
            new(0, "S1", "D1", PlanStepStatus.Completed, null, null, null),
            new(1, "S2", "D2", PlanStepStatus.InProgress, null, null, null),
        };
        var plan = new PlanInfo("p1", "goal", "desc", PlanStatus.InProgress, 0.5, 4, 1, DateTime.UtcNow, null);
        kernelClient.Setup(k => k.GetCurrentPlanAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlanExecutionResult("p1", true, plan, steps, null));

        var vm = new PlanViewModel(kernelClient.Object);
        await vm.LoadPlanAsync();
        Assert.Equal(0.5, vm.Progress);
    }

    [Fact]
    public async Task RefreshAsync_ShouldReloadPlan()
    {
        var kernelClient = new Mock<IKernelClient>();
        var plan = new PlanInfo("p1", "g", "d", PlanStatus.Completed, 1.0, 2, 2, DateTime.UtcNow, null);
        kernelClient.Setup(k => k.GetCurrentPlanAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlanExecutionResult("p1", false, plan, [], null));

        var vm = new PlanViewModel(kernelClient.Object);
        await vm.RefreshAsync();

        Assert.NotNull(vm.CurrentPlan);
        Assert.Equal(PlanStatus.Completed, vm.CurrentPlan!.Status);
    }
}
