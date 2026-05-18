using KrnlAI.Desktop.App;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class LoginViewModelTests
{
    [Fact]
    public void LoginViewModel_ErrorMessageChanged_ShouldUpdateHasError()
    {
        var sut = new LoginViewModel();

        Assert.False(sut.HasError);

        sut.ErrorMessage = "Login falhou";

        Assert.True(sut.HasError);

        sut.ErrorMessage = string.Empty;

        Assert.False(sut.HasError);
    }
}
