namespace KrnlAI.Sidecar.Tests;

public sealed class ApiVersionTests_Legacy(SidecarWebAppFactory factory) : IClassFixture<SidecarWebAppFactory>
{
    private readonly HttpClient _http = factory.CreateClient();

    [Fact]
    public async Task ApiVersion_LegacyMode_ShouldReturnHeader()
    {
        var res = await _http.GetAsync("/health", TestContext.Current.CancellationToken);

        res.Headers.Should().Contain(h => h.Key == "X-API-Version");
        res.Headers.GetValues("X-API-Version").First().Should().Be("1.0");
    }
}

public sealed class ApiVersionTests_Community(CommunitySidecarWebAppFactory factory) : IClassFixture<CommunitySidecarWebAppFactory>
{
    private readonly HttpClient _http = factory.CreateClient();

    [Fact]
    public async Task ApiVersion_CommunityMode_ShouldReturnHeader()
    {
        var res = await _http.GetAsync("/health", TestContext.Current.CancellationToken);

        res.Headers.Should().Contain(h => h.Key == "X-API-Version");
        res.Headers.GetValues("X-API-Version").First().Should().Be("1.0");
    }
}
