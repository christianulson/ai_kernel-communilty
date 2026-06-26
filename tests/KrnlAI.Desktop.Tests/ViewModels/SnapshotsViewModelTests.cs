namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class SnapshotsViewModelTests
{
    [Fact]
    public void IsLoading_Default_ShouldBeFalse()
    {
        var vm = new SnapshotsViewModel();
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void HasError_Default_ShouldBeFalse()
    {
        var vm = new SnapshotsViewModel();
        Assert.False(vm.HasError);
    }

    [Fact]
    public void Snapshots_Default_ShouldBeEmpty()
    {
        var vm = new SnapshotsViewModel();
        Assert.Empty(vm.Snapshots);
    }

    [Fact]
    public void HasNoData_WhenNotLoadingAndEmpty_ShouldBeTrue()
    {
        var vm = new SnapshotsViewModel();
        Assert.True(vm.HasNoData);
    }

    [Fact]
    public void ErrorMessage_WhenSet_ShouldUpdateHasError()
    {
        var vm = new SnapshotsViewModel();
        vm.ErrorMessage = "error";
        Assert.True(vm.HasError);
        Assert.False(vm.HasNoData);
    }
}
