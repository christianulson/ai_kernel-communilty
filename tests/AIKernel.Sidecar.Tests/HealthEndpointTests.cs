namespace AIKernel.Sidecar.Tests;

public sealed class HealthEndpointTests(SidecarWebAppFactory factory) : IClassFixture<SidecarWebAppFactory>
{
    private readonly HttpClient _http = factory.CreateClient();

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/health/live")]
    public async Task HealthEndpoint_ShouldReturn200(string url)
    {
        var res = await _http.GetAsync(url, TestContext.Current.CancellationToken);
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealth_ShouldReturnJsonWithStatus()
    {
        var res = await _http.GetAsync("/health", TestContext.Current.CancellationToken);
        var body = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.ContainsKey("status").Should().BeTrue();
        body.ContainsKey("version").Should().BeTrue();
    }

    [Fact]
    public async Task GetHealth_ShouldHaveVersion()
    {
        var res = await _http.GetAsync("/health", TestContext.Current.CancellationToken);
        var body = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: TestContext.Current.CancellationToken);
        body!["version"].ToString().Should().Be("AIKernel.Sidecar/1.0.0");
    }
}
