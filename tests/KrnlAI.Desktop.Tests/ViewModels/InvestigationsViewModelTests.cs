namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class InvestigationsViewModelTests
{
    [Fact]
    public void IsLoading_Default_ShouldBeFalse()
    {
        var vm = new InvestigationsViewModel();
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void HasError_Default_ShouldBeFalse()
    {
        var vm = new InvestigationsViewModel();
        Assert.False(vm.HasError);
    }

    [Fact]
    public void Investigations_Default_ShouldBeEmpty()
    {
        var vm = new InvestigationsViewModel();
        Assert.Empty(vm.Investigations);
    }

    [Fact]
    public void HasNoData_WhenNotLoadingAndEmpty_ShouldBeTrue()
    {
        var vm = new InvestigationsViewModel();
        Assert.True(vm.HasNoData);
    }
}
