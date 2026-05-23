using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace KrnlAI.Sidecar.Tests;

public sealed class RateLimitingTests : IClassFixture<RateLimitSidecarWebAppFactory>
{
    private readonly HttpClient _http;

    public RateLimitingTests(RateLimitSidecarWebAppFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task RateLimiting_AgentRun_AfterExceedingLimit_ShouldReturn429()
    {
        var payload = new { prompt = "hello" };
        var tasks = Enumerable.Range(0, 5).Select(_ =>
            _http.PostAsJsonAsync("/agent/run", payload, TestContext.Current.CancellationToken));
        var results = await Task.WhenAll(tasks);

        var has429 = results.Any(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests);
        has429.Should().BeTrue("rate limiting should block excess requests");
    }

    [Fact]
    public async Task RateLimiting_Health_ShouldNotBeRateLimited()
    {
        var tasks = Enumerable.Range(0, 20).Select(_ =>
            _http.GetAsync("/health", TestContext.Current.CancellationToken));
        var results = await Task.WhenAll(tasks);

        results.All(r => r.IsSuccessStatusCode).Should().BeTrue("health endpoint should not be rate limited");
    }
}

public sealed class RateLimitSidecarWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("https_port", "5001");
        builder.UseSetting("Sidecar:RateLimiting:AgentRunPermitLimit", "2");
        builder.UseSetting("Sidecar:RateLimiting:WindowSeconds", "60");
        builder.UseSetting("Sidecar:RateLimiting:MemoryReadPermitLimit", "30");
        builder.UseSetting("Sidecar:RateLimiting:GlobalPermitLimit", "60");
    }
}
