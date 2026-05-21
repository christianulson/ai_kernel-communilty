using KrnlAI.Desktop.App;
using Xunit;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class LoginViewModelExtendedTests
{
    [Fact]
    public void LoginViewModel_HasError_WhenEmpty_ShouldBeFalse()
    {
        var vm = new LoginViewModel();
        Assert.False(vm.HasError);
    }

    [Fact]
    public void LoginViewModel_ErrorMessage_ShouldUpdateHasError()
    {
        var vm = new LoginViewModel();
        vm.ErrorMessage = "test error";
        Assert.True(vm.HasError);
    }

    [Fact]
    public void LoginViewModel_RememberMe_ShouldRoundTrip()
    {
        var vm = new LoginViewModel();
        vm.RememberMe = true;
        Assert.True(vm.RememberMe);
        vm.RememberMe = false;
        Assert.False(vm.RememberMe);
    }
}
