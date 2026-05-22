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

    [Fact]
    public async Task SendAsync_On401WithRefreshHandler_ShouldRetryWithNewToken()
    {
        var tokenProvider = new AuthTokenProvider
        {
            Token = "expired-token",
            RefreshToken = "valid-refresh-token"
        };

        var callCount = 0;
        Task<string?> RefreshHandler(CancellationToken _)
        {
            callCount++;
            tokenProvider.Token = "refreshed-token";
            return Task.FromResult<string?>("refreshed-token");
        }

        var handler = new AuthTokenHandler(tokenProvider, RefreshHandler);
        handler.InnerHandler = new SequentialHandler(
            new HttpResponseMessage(HttpStatusCode.Unauthorized),
            new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");
        var response = await httpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, callCount);
        Assert.Equal("refreshed-token", tokenProvider.Token);
    }

    [Fact]
    public async Task SendAsync_On401WithoutRefreshToken_ShouldNotRetry()
    {
        var tokenProvider = new AuthTokenProvider
        {
            Token = "expired-token",
            RefreshToken = null
        };

        var refreshCalled = false;
        Task<string?> RefreshHandler(CancellationToken _)
        {
            refreshCalled = true;
            return Task.FromResult<string?>("refreshed-token");
        }

        var handler = new AuthTokenHandler(tokenProvider, RefreshHandler);
        handler.InnerHandler = new TestMessageHandler(HttpStatusCode.Unauthorized);

        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");
        var response = await httpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(refreshCalled);
    }

    [Fact]
    public async Task SendAsync_OnRefreshEndpoint_ShouldNotIntercept()
    {
        var tokenProvider = new AuthTokenProvider
        {
            Token = "expired-token",
            RefreshToken = "valid-refresh-token"
        };

        var refreshCalled = false;
        Task<string?> RefreshHandler(CancellationToken _)
        {
            refreshCalled = true;
            return Task.FromResult<string?>("new-token");
        }

        var handler = new AuthTokenHandler(tokenProvider, RefreshHandler);
        handler.InnerHandler = new TestMessageHandler(HttpStatusCode.Unauthorized);

        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/auth/refresh");
        var response = await httpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(refreshCalled);
    }

    [Fact]
    public async Task SendAsync_OnOAuth2CallbackEndpoint_ShouldNotIntercept()
    {
        var tokenProvider = new AuthTokenProvider
        {
            Token = "expired-token",
            RefreshToken = "valid-refresh-token"
        };

        var refreshCalled = false;
        Task<string?> RefreshHandler(CancellationToken _)
        {
            refreshCalled = true;
            return Task.FromResult<string?>("new-token");
        }

        var handler = new AuthTokenHandler(tokenProvider, RefreshHandler);
        handler.InnerHandler = new TestMessageHandler(HttpStatusCode.Unauthorized);

        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/auth/oauth2/callback");
        var response = await httpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(refreshCalled);
    }

    [Fact]
    public async Task SendAsync_WhenRefreshFails_ShouldReturnOriginal401()
    {
        var tokenProvider = new AuthTokenProvider
        {
            Token = "expired-token",
            RefreshToken = "invalid-refresh-token"
        };

        Task<string?> RefreshHandler(CancellationToken _) => Task.FromResult<string?>(null);

        var handler = new AuthTokenHandler(tokenProvider, RefreshHandler);
        handler.InnerHandler = new TestMessageHandler(HttpStatusCode.Unauthorized);

        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");
        var response = await httpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(tokenProvider.Token);
        Assert.Null(tokenProvider.RefreshToken);
    }

    private class TestMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        public HttpRequestMessage? LastRequest { get; private set; }

        public TestMessageHandler(HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(_statusCode));
        }
    }

    private class SequentialHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public SequentialHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responses.Count > 0 ? _responses.Dequeue() : new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
