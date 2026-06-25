using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MemoryCache = Microsoft.Extensions.Caching.Memory.MemoryCache;

namespace KrnlAI.Sidecar.Tests;

public sealed class KernelApiProxyTests
{
    private static (KernelApiProxy Proxy, MockHttpHandler Handler) CreateProxy(string baseUrl = "http://localhost:5000")
    {
        var handler = new MockHttpHandler();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new SidecarOptions
        {
            KernelApi = new KernelApiOptions { BaseUrl = baseUrl, TimeoutSeconds = 10, CacheTtlSeconds = 60 }
        });

        if (string.IsNullOrEmpty(baseUrl))
        {
            var httpClient = new HttpClient(handler);
            var factory = new MockHttpClientFactory(httpClient);
            var proxy = new KernelApiProxy(factory, cache, options, NullLogger<KernelApiProxy>.Instance);
            return (proxy, handler);
        }

        var httpClient2 = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
        var factory2 = new MockHttpClientFactory(httpClient2);
        var proxy2 = new KernelApiProxy(factory2, cache, options, NullLogger<KernelApiProxy>.Instance);
        return (proxy2, handler);
    }

    [Fact]
    public async Task Proxy_NotConfigured_ShouldReturnNull()
    {
        var (proxy, _) = CreateProxy("");
        var result = await proxy.ProxyGetAsync<object>("/test", CancellationToken.None).ConfigureAwait(false);
        result.Should().BeNull();
    }

    [Fact]
    public async Task Proxy_ConfiguredAndOnline_ShouldReturnData()
    {
        var (proxy, handler) = CreateProxy();
        handler.SetupResponse("/health", new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"status":"ok"}""")
        });

        var result = await proxy.ProxyGetAsync<HealthResponse>("/health", CancellationToken.None).ConfigureAwait(false);
        result.Should().NotBeNull();
        result!.Status.Should().Be("ok");
    }

    [Fact]
    public async Task Proxy_ConfiguredAndOffline_ShouldReturnNull()
    {
        var (proxy, handler) = CreateProxy();
        handler.SetupResponse("/test", new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var result = await proxy.ProxyGetAsync<object>("/test", CancellationToken.None).ConfigureAwait(false);
        result.Should().BeNull();
    }

    [Fact]
    public async Task Proxy_Ping_WhenConfiguredAndOnline_ShouldReturnTrue()
    {
        var (proxy, handler) = CreateProxy();
        handler.SetupResponse("/health", new HttpResponseMessage(HttpStatusCode.OK));

        var result = await proxy.PingAsync(CancellationToken.None).ConfigureAwait(false);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Proxy_Ping_WhenConfiguredAndOffline_ShouldReturnFalse()
    {
        var (proxy, handler) = CreateProxy();
        handler.SetupResponse("/health", new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var result = await proxy.PingAsync(CancellationToken.None).ConfigureAwait(false);
        result.Should().BeFalse();
    }

    private sealed record HealthResponse(string Status);

    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, HttpResponseMessage> _responses = [];

        public void SetupResponse(string path, HttpResponseMessage response)
            => _responses[path] = response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            return Task.FromResult(_responses.GetValueOrDefault(path, new HttpResponseMessage(HttpStatusCode.NotFound)));
        }
    }

    private sealed class MockHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }
}
