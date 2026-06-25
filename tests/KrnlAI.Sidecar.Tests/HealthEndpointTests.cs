namespace KrnlAI.Sidecar.Tests;

public sealed class HealthEndpointTests(SidecarWebAppFactory factory) : IClassFixture<SidecarWebAppFactory>
{
    private readonly HttpClient _http = factory.CreateClient();

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/health/live")]
    public async Task HealthEndpoint_ShouldReturn200(string url)
    {
        var res = await _http.GetAsync(url, TestContext.Current.CancellationToken).ConfigureAwait(false);
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealth_ShouldReturnJsonWithStatus()
    {
        var res = await _http.GetAsync("/health", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var body = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(false);
        body.Should().NotBeNull();
        body!.ContainsKey("status").Should().BeTrue();
        body.ContainsKey("version").Should().BeTrue();
    }

    [Fact]
    public async Task GetHealth_ShouldHaveVersion()
    {
        var res = await _http.GetAsync("/health", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var body = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(false);
        body!["version"].ToString().Should().Be("KrnlAI.Sidecar/1.0.0");
    }
}
