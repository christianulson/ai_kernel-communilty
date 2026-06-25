namespace KrnlAI.Sidecar.Tests;

public sealed class SecurityHeadersTests(SidecarWebAppFactory factory) : IClassFixture<SidecarWebAppFactory>
{
    private readonly HttpClient _http = factory.CreateClient();

    [Fact]
    public async Task SecurityHeaders_AgentRun_ShouldHaveCSP()
    {
        var res = await _http.PostAsJsonAsync("/agent/run", new { prompt = "hello" }, TestContext.Current.CancellationToken).ConfigureAwait(false);
        res.Headers.Should().Contain(h => h.Key == "Content-Security-Policy");
        var csp = res.Headers.GetValues("Content-Security-Policy").First();
        csp.Should().Contain("default-src 'none'");
    }

    [Fact]
    public async Task SecurityHeaders_AgentRun_ShouldHaveXFrameOptions()
    {
        var res = await _http.PostAsJsonAsync("/agent/run", new { prompt = "hello" }, TestContext.Current.CancellationToken).ConfigureAwait(false);
        res.Headers.Should().Contain(h => h.Key == "X-Frame-Options");
        res.Headers.GetValues("X-Frame-Options").First().Should().Be("DENY");
    }

    [Fact]
    public async Task SecurityHeaders_AgentRun_ShouldHaveXContentTypeOptions()
    {
        var res = await _http.PostAsJsonAsync("/agent/run", new { prompt = "hello" }, TestContext.Current.CancellationToken).ConfigureAwait(false);
        res.Headers.Should().Contain(h => h.Key == "X-Content-Type-Options");
        res.Headers.GetValues("X-Content-Type-Options").First().Should().Be("nosniff");
    }

    [Fact]
    public async Task SecurityHeaders_AgentRun_ShouldHavePermissionsPolicy()
    {
        var res = await _http.PostAsJsonAsync("/agent/run", new { prompt = "hello" }, TestContext.Current.CancellationToken).ConfigureAwait(false);
        res.Headers.Should().Contain(h => h.Key == "Permissions-Policy");
    }

    [Fact]
    public async Task SecurityHeaders_AgentRun_ShouldHaveReferrerPolicy()
    {
        var res = await _http.PostAsJsonAsync("/agent/run", new { prompt = "hello" }, TestContext.Current.CancellationToken).ConfigureAwait(false);
        res.Headers.Should().Contain(h => h.Key == "Referrer-Policy");
    }

    [Fact]
    public async Task SecurityHeaders_Health_ShouldAlsoHaveXFrameOptions()
    {
        var res = await _http.GetAsync("/health", TestContext.Current.CancellationToken).ConfigureAwait(false);
        res.Headers.Should().Contain(h => h.Key == "X-Frame-Options");
    }
}
