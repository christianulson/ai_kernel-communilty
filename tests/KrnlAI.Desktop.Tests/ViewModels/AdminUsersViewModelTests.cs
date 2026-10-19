namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class AdminUsersViewModelTests
{
    [Fact]
    public void IsLoading_Default_ShouldBeFalse()
    {
        var vm = new AdminUsersViewModel();
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void Users_Default_ShouldBeEmpty()
    {
        var vm = new AdminUsersViewModel();
        Assert.Empty(vm.Users);
    }

    [Fact]
    public void HasSelection_Default_ShouldBeFalse()
    {
        var vm = new AdminUsersViewModel();
        Assert.False(vm.HasSelection);
    }

    [Fact]
    public void StatusMessage_Default_ShouldBeEmpty()
    {
        var vm = new AdminUsersViewModel();
        Assert.Equal("", vm.StatusMessage);
    }

    [Fact]
    public void SelectedUser_WhenSet_ShouldUpdateHasSelection()
    {
        var vm = new AdminUsersViewModel();
        vm.SelectedUser = new KrnlAI.Desktop.Infrastructure.Abstractions.UserInfo("1", "Test", "t@t.com", "user", true, DateTime.UtcNow);
        Assert.True(vm.HasSelection);
    }

    [Fact]
    public void SelectedUser_WhenCleared_ShouldUpdateHasSelection()
    {
        var vm = new AdminUsersViewModel();
        vm.SelectedUser = new KrnlAI.Desktop.Infrastructure.Abstractions.UserInfo("1", "Test", "t@t.com", "user", true, DateTime.UtcNow);
        vm.SelectedUser = null;
        Assert.False(vm.HasSelection);
    }

    [Fact]
    public void LoadCommand_ShouldExist()
    {
        var vm = new AdminUsersViewModel();
        Assert.NotNull(vm.LoadCommand);
        Assert.True(vm.LoadCommand.CanExecute(null));
    }

    [Fact]
    public void ActivateCommand_WhenNoSelection_ShouldNotThrow()
    {
        var vm = new AdminUsersViewModel();
        vm.SelectedUser = null;
        var ex = Record.Exception(() => vm.ActivateCommand.Execute(null));
        Assert.Null(ex);
    }

    [Fact]
    public void SuspendCommand_WhenNoSelection_ShouldNotThrow()
    {
        var vm = new AdminUsersViewModel();
        vm.SelectedUser = null;
        var ex = Record.Exception(() => vm.SuspendCommand.Execute(null));
        Assert.Null(ex);
    }

    [Fact]
    public void IsLoading_WhenSet_ShouldUpdate()
    {
        var vm = new AdminUsersViewModel();
        vm.IsLoading = true;
        Assert.True(vm.IsLoading);
        vm.IsLoading = false;
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void StatusMessage_WhenSet_ShouldRoundTrip()
    {
        var vm = new AdminUsersViewModel();
        vm.StatusMessage = "loaded";
        Assert.Equal("loaded", vm.StatusMessage);
    }
}
