namespace KrnlAI.Sidecar.Tests;

public sealed class CorrelationIdTests(SidecarWebAppFactory factory) : IClassFixture<SidecarWebAppFactory>
{
    private readonly HttpClient _http = factory.CreateClient();

    [Fact]
    public async Task CorrelationId_WithoutHeader_ShouldGenerate()
    {
        var res = await _http.GetAsync("/health", TestContext.Current.CancellationToken).ConfigureAwait(false);

        res.Headers.Should().Contain(h => h.Key == "X-Request-ID");
        var rid = res.Headers.GetValues("X-Request-ID").First();
        rid.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CorrelationId_WithHeader_ShouldEcho()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Request-ID", "abc-123");

        var res = await _http.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(false);

        res.Headers.GetValues("X-Request-ID").First().Should().Be("abc-123");
    }

    [Fact]
    public async Task CorrelationId_WithCorrelationHeader_ShouldEcho()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Correlation-ID", "def-456");

        var res = await _http.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(false);

        res.Headers.GetValues("X-Request-ID").First().Should().Be("def-456");
    }
}
