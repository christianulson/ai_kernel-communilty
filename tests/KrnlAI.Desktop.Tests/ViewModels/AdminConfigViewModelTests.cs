namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class AdminConfigViewModelTests
{
    [Fact]
    public void IsLoading_Default_ShouldBeFalse()
    {
        var vm = new AdminConfigViewModel();
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void FeatureFlags_Default_ShouldBeEmpty()
    {
        var vm = new AdminConfigViewModel();
        Assert.Empty(vm.FeatureFlags);
    }

    [Fact]
    public void ConfigEntries_Default_ShouldBeEmpty()
    {
        var vm = new AdminConfigViewModel();
        Assert.Empty(vm.ConfigEntries);
    }

    [Fact]
    public void StatusMessage_Default_ShouldBeEmpty()
    {
        var vm = new AdminConfigViewModel();
        Assert.Equal("", vm.StatusMessage);
    }

    [Fact]
    public void StatusMessage_WhenSet_ShouldRoundTrip()
    {
        var vm = new AdminConfigViewModel();
        vm.StatusMessage = "test message";
        Assert.Equal("test message", vm.StatusMessage);
    }

    [Fact]
    public void IsLoading_WhenSet_ShouldUpdate()
    {
        var vm = new AdminConfigViewModel();
        vm.IsLoading = true;
        Assert.True(vm.IsLoading);
        vm.IsLoading = false;
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void LoadCommand_ShouldExist()
    {
        var vm = new AdminConfigViewModel();
        Assert.NotNull(vm.LoadCommand);
        Assert.True(vm.LoadCommand.CanExecute(null));
    }
}
