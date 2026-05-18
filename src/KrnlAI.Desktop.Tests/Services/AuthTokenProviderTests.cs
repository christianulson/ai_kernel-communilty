using KrnlAI.Desktop.Infrastructure.KernelClient;

namespace KrnlAI.Desktop.Tests.Services;

public class AuthTokenProviderTests
{
    [Fact]
    public void SetAndGetToken_ShouldWork()
    {
        var provider = new AuthTokenProvider();
        Assert.Null(provider.Token);

        provider.Token = "my-token";
        Assert.Equal("my-token", provider.Token);

        provider.Token = null;
        Assert.Null(provider.Token);
    }
}
