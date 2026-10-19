namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class DisputesViewModelTests
{
    [Fact]
    public void Disputes_Default_ShouldBeEmpty()
    {
        var vm = new DisputesViewModel();
        Assert.Empty(vm.Disputes);
    }

    [Fact]
    public void SelectedDispute_Default_ShouldBeNull()
    {
        var vm = new DisputesViewModel();
        Assert.Null(vm.SelectedDispute);
    }

    [Fact]
    public void HasSelectedDispute_WhenNull_ShouldBeFalse()
    {
        var vm = new DisputesViewModel();
        Assert.False(vm.HasSelectedDispute);
    }

    [Fact]
    public void HasSelectedDispute_WhenSet_ShouldBeTrue()
    {
        var vm = new DisputesViewModel();
        vm.SelectedDispute = new DisputeItem("d1", "w1", "node1", "reason", "open", DateTime.UtcNow);
        Assert.True(vm.HasSelectedDispute);
    }

    [Fact]
    public void SelectedDispute_WhenCleared_ShouldUpdateHasSelected()
    {
        var vm = new DisputesViewModel();
        vm.SelectedDispute = new DisputeItem("d1", "w1", "node1", "reason", "open", DateTime.UtcNow);
        vm.SelectedDispute = null;
        Assert.False(vm.HasSelectedDispute);
    }

    [Fact]
    public void RefreshCommand_ShouldExist()
    {
        var vm = new DisputesViewModel();
        Assert.NotNull(vm.RefreshCommand);
    }

    [Fact]
    public void ResolveForWorkerCommand_ShouldExist()
    {
        var vm = new DisputesViewModel();
        Assert.NotNull(vm.ResolveForWorkerCommand);
    }

    [Fact]
    public void ResolveForSolicitorCommand_ShouldExist()
    {
        var vm = new DisputesViewModel();
        Assert.NotNull(vm.ResolveForSolicitorCommand);
    }
}
