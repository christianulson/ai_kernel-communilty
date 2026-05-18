using System.Net;
using KrnlAI.Desktop.Infrastructure.KernelClient;

namespace KrnlAI.Desktop.Tests.Services;

public class AuthTokenHandlerTests
{
    [Fact]
    public async Task SendAsync_WithToken_AddsBearerHeader()
    {
        var tokenProvider = new AuthTokenProvider { Token = "test-jwt-token" };
        var handler = new AuthTokenHandler(tokenProvider);
        var innerHandler = new TestMessageHandler();
        handler.InnerHandler = innerHandler;

        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");
        await httpClient.SendAsync(request);

        Assert.NotNull(innerHandler.LastRequest);
        Assert.Equal("Bearer test-jwt-token", innerHandler.LastRequest!.Headers.Authorization?.ToString());
    }

    [Fact]
    public async Task SendAsync_WithoutToken_DoesNotAddHeader()
    {
        var tokenProvider = new AuthTokenProvider { Token = null };
        var handler = new AuthTokenHandler(tokenProvider);
        var innerHandler = new TestMessageHandler();
        handler.InnerHandler = innerHandler;

        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");
        await httpClient.SendAsync(request);

        Assert.NotNull(innerHandler.LastRequest);
        Assert.Null(innerHandler.LastRequest!.Headers.Authorization);
    }

    private class TestMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
