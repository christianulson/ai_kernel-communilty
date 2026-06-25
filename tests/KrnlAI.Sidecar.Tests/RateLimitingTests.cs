using Microsoft.AspNetCore.Mvc.Testing;

namespace KrnlAI.Sidecar.Tests;

public sealed class RateLimitingTests(RateLimitSidecarWebAppFactory factory) : IClassFixture<RateLimitSidecarWebAppFactory>
{
    private readonly HttpClient _http = factory.CreateClient();

    [Fact]
    public async Task RateLimiting_AgentRun_AfterExceedingLimit_ShouldReturn429()
    {
        var payload = new { prompt = "hello" };

        // First request consumes 1 of 2 permits
        var res1 = await _http.PostAsJsonAsync("/agent/run", payload, TestContext.Current.CancellationToken).ConfigureAwait(false);
        res1.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Second request consumes 2nd permit
        var res2 = await _http.PostAsJsonAsync("/agent/run", payload, TestContext.Current.CancellationToken).ConfigureAwait(false);
        res2.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Third request exceeds limit
        var res3 = await _http.PostAsJsonAsync("/agent/run", payload, TestContext.Current.CancellationToken).ConfigureAwait(false);
        res3.StatusCode.Should().Be(System.Net.HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task RateLimiting_Health_ShouldNotBeRateLimited()
    {
        for (var i = 0; i < 20; i++)
        {
            var res = await _http.GetAsync("/health", TestContext.Current.CancellationToken).ConfigureAwait(false);
            res.IsSuccessStatusCode.Should().BeTrue($"health request {i + 1} should succeed");
        }
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
