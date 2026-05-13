using System.Net.Http.Json;
using FluentAssertions;

namespace AIKernel.Sidecar.Tests;

public sealed class HealthEndpointTests(SidecarWebAppFactory factory) : IClassFixture<SidecarWebAppFactory>
{
    private readonly HttpClient _http = factory.CreateClient();

    [Fact]
    public async Task GetHealth_ShouldReturn200_WithStatusOk()
    {
        var res = await _http.GetAsync("/health", TestContext.Current.CancellationToken);

        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!["status"].ToString().Should().Be("ok");
        body["version"].ToString().Should().Be("AIKernel.Sidecar/1.0.0");
    }

    [Fact]
    public async Task GetHealth_ShouldHaveTs()
    {
        var res = await _http.GetAsync("/health", TestContext.Current.CancellationToken);

        var body = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().ContainKey("ts");
    }
}
