using KrnlAI.Desktop.App;
using KrnlAI.Desktop.Core.Abstractions;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class LoginOAuthContractTests
{
    [Fact]
    public void LoginViewModel_GetOAuthBrowserUri_ShouldPreferProviderAuthUrl()
    {
        var response = new OAuth2LoginResponse("https://login.example.test/authorize?state=abc", null);

        var uri = LoginViewModel.GetOAuthBrowserUri(response, "http://localhost:49813/oauth/callback");

        Assert.Equal("https://login.example.test/authorize?state=abc", uri.ToString());
    }
}
