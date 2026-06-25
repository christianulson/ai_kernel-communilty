using Microsoft.AspNetCore.Mvc.Testing;

namespace KrnlAI.Sidecar.Tests;

public sealed class CorsTests(CorsSidecarWebAppFactory factory) : IClassFixture<CorsSidecarWebAppFactory>
{
    private readonly HttpClient _http = factory.CreateClient();

    [Fact]
    public async Task Cors_WithAllowedOrigin_ShouldReturnAllowOriginHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/health");
        request.Headers.Add("Origin", "https://example.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var res = await _http.SendAsync(request, TestContext.Current.CancellationToken);

        res.Headers.Should().Contain(h => h.Key == "Access-Control-Allow-Origin");
        res.Headers.GetValues("Access-Control-Allow-Origin").First().Should().Be("https://example.com");
    }

    [Fact]
    public async Task Cors_WithDisallowedOrigin_ShouldNotReturnAllowOriginHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/health");
        request.Headers.Add("Origin", "https://evil.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var res = await _http.SendAsync(request, TestContext.Current.CancellationToken);

        res.Headers.Should().NotContain(h => h.Key == "Access-Control-Allow-Origin");
    }
}

public sealed class CorsSidecarWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("https_port", "5001");
        builder.UseSetting("Sidecar:Cors:AllowedOrigins:0", "https://example.com");
    }
}
