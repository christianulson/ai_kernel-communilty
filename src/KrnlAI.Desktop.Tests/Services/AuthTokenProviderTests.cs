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

    [Fact]
    public void SetTokens_ShouldStoreBoth()
    {
        var provider = new AuthTokenProvider();

        provider.SetTokens("access-token", "refresh-token");

        Assert.Equal("access-token", provider.Token);
        Assert.Equal("refresh-token", provider.RefreshToken);
    }

    [Fact]
    public void Clear_ShouldClearBoth()
    {
        var provider = new AuthTokenProvider();
        provider.SetTokens("access-token", "refresh-token");

        provider.Clear();

        Assert.Null(provider.Token);
        Assert.Null(provider.RefreshToken);
    }

    [Fact]
    public void SetTokens_WithNull_ShouldClear()
    {
        var provider = new AuthTokenProvider();
        provider.SetTokens("access-token", "refresh-token");

        provider.SetTokens(null, null);

        Assert.Null(provider.Token);
        Assert.Null(provider.RefreshToken);
    }

    [Fact]
    public void TokenAndRefreshToken_ShouldBeThreadSafe()
    {
        var provider = new AuthTokenProvider();
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        Parallel.For(0, 100, i =>
        {
            try
            {
                if (i % 2 == 0)
                    provider.Token = $"token-{i}";
                else
                    provider.RefreshToken = $"refresh-{i}";
                _ = provider.Token;
                _ = provider.RefreshToken;
            }
            catch (Exception ex) { exceptions.Add(ex); }
        });

        Assert.Empty(exceptions);
    }
}
