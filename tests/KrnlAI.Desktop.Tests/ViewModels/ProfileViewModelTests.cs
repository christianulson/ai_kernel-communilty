using KrnlAI.Desktop.App.ViewModels;
using Xunit;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class ProfileViewModelTests
{
    [Fact] public void IsLoading_Default_ShouldBeFalse() => Assert.False(new ProfileViewModel().IsLoading);
    [Fact] public void IsSaving_Default_ShouldBeFalse() => Assert.False(new ProfileViewModel().IsSaving);
    [Fact] public void UserId_ShouldRoundTrip()
    {
        var vm = new ProfileViewModel();
        vm.UserId = "u1";
        Assert.Equal("u1", vm.UserId);
    }
    [Fact] public void Name_ShouldRoundTrip()
    {
        var vm = new ProfileViewModel();
        vm.Name = "Test";
        Assert.Equal("Test", vm.Name);
    }
}
