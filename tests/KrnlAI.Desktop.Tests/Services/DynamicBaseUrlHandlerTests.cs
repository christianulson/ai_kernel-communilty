namespace KrnlAI.Desktop.Tests.Services;

public sealed class DynamicBaseUrlHandlerTests
{
    [Fact]
    public async Task SetBaseUrl_ShouldRewriteRequest()
    {
        DynamicBaseUrlHandler.SetBaseUrl("http://test-api:8080");
        var handler = new DynamicBaseUrlHandler();
        var testHandler = new CapturingHandler();
        handler.InnerHandler = testHandler;

        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://placeholder") };
        await client.GetAsync("/health");

        Assert.StartsWith("http://test-api:8080/health", testHandler.LastUri?.ToString());
    }

    [Fact]
    public async Task DefaultBaseUrl_ShouldTargetLocalApi()
    {
        DynamicBaseUrlHandler.ResetBaseUrl();
        var handler = new DynamicBaseUrlHandler();
        var testHandler = new CapturingHandler();
        handler.InnerHandler = testHandler;

        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://placeholder") };
        await client.GetAsync("/test");

        Assert.StartsWith("http://localhost:5235/test", testHandler.LastUri?.ToString());
    }

    [Fact]
    public async Task SetBaseUrl_ShouldTrimTrailingSlash()
    {
        DynamicBaseUrlHandler.SetBaseUrl("http://example.com/api/");
        var handler = new DynamicBaseUrlHandler();
        var testHandler = new CapturingHandler();
        handler.InnerHandler = testHandler;

        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://placeholder") };
        await client.GetAsync("/ping");

        Assert.StartsWith("http://example.com/api/ping", testHandler.LastUri?.ToString());
        Assert.DoesNotContain("//ping", testHandler.LastUri?.ToString());
    }

    private class CapturingHandler : HttpMessageHandler
    {
        public Uri? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
