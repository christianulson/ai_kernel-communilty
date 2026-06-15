using KrnlAI.Desktop.App.ViewModels;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class ObjectivesViewModelTests
{
    [Fact]
    public void IsLoading_Default_ShouldBeFalse()
    {
        var vm = new ObjectivesViewModel();
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void HasError_Default_ShouldBeFalse()
    {
        var vm = new ObjectivesViewModel();
        Assert.False(vm.HasError);
    }

    [Fact]
    public void Objectives_Default_ShouldBeEmpty()
    {
        var vm = new ObjectivesViewModel();
        Assert.Empty(vm.Objectives);
    }

    [Fact]
    public void HasNoData_WhenNotLoadingAndEmpty_ShouldBeTrue()
    {
        var vm = new ObjectivesViewModel();
        Assert.True(vm.HasNoData);
    }

    [Fact]
    public void ErrorMessage_WhenSet_ShouldUpdateHasError()
    {
        var vm = new ObjectivesViewModel();
        vm.ErrorMessage = "error";
        Assert.True(vm.HasError);
        Assert.False(vm.HasNoData);
    }

    [Fact]
    public void ClearDetailCommand_ShouldNullifySelection()
    {
        var vm = new ObjectivesViewModel();
        vm.ClearDetailCommand.Execute(null);
        Assert.Null(vm.SelectedObjective);
    }
}
