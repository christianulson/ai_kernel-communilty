using System.Net;
using System.Net.Http;
using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

public sealed class KernelClientServiceTests
{
    [Fact]
    public void KernelEndpointResolver_EmbeddedMode_ShouldUseSidecarEndpoint()
    {
        var endpoint = KernelEndpointResolver.Resolve(KernelRuntimeMode.Embedded, "https://api.krnlai.dev", 9100);

        endpoint.Should().Be("http://127.0.0.1:9100");
    }

    [Fact]
    public void KernelEndpointResolver_LocalApiMode_ShouldRejectRemoteEndpoint()
    {
        var endpoint = KernelEndpointResolver.Resolve(KernelRuntimeMode.LocalApi, "https://api.krnlai.dev", 5001);

        endpoint.Should().Be("http://localhost:5235");
    }

    [Fact]
    public void KernelEndpointResolver_RemoteApiMode_ShouldAllowRemoteEndpoint()
    {
        var endpoint = KernelEndpointResolver.Resolve(KernelRuntimeMode.RemoteApi, "https://api.krnlai.dev", 5001);

        endpoint.Should().Be("https://api.krnlai.dev");
    }

    [Fact]
    public void State_AfterConstruction_ShouldBeDisconnected()
    {
        using var service = new KernelClientService();
        service.State.Should().Be(ConnectionState.Disconnected);
    }

    [Fact]
    public async Task ConnectAsync_WithHealthyEndpoint_ShouldReturnConnected()
    {
        using var handler = new MockHttpHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath == "/health")
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"ok\": true, \"ts\": \"2026-01-01T00:00:00Z\"}"),
                };
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler);
        using var service = new KernelClientService(http);

        var result = await service.ConnectAsync("http://localhost:65335");

        result.Should().BeTrue();
        service.State.Should().Be(ConnectionState.Connected);
    }

    [Fact]
    public async Task ConnectAsync_WithUnhealthyEndpoint_ShouldReturnFailed()
    {
        using var handler = new MockHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        using var http = new HttpClient(handler);
        using var service = new KernelClientService(http);

        var result = await service.ConnectAsync("http://localhost:65335");

        result.Should().BeFalse();
        service.State.Should().Be(ConnectionState.Failed);
    }

    [Fact]
    public async Task DisconnectAsync_AfterConnect_ShouldResetState()
    {
        using var handler = new MockHttpHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath == "/health")
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"ok\": true, \"ts\": \"2026-01-01T00:00:00Z\"}"),
                };
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler);
        using var service = new KernelClientService(http);

        await service.ConnectAsync("http://localhost:65335");
        await service.DisconnectAsync();

        service.State.Should().Be(ConnectionState.Disconnected);
    }

    [Fact]
    public async Task StateChanged_ShouldFireOnTransition()
    {
        using var handler = new MockHttpHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath == "/health")
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"ok\": true, \"ts\": \"2026-01-01T00:00:00Z\"}"),
                };
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler);
        using var service = new KernelClientService(http);
        var states = new System.Collections.Generic.List<ConnectionState>();
        service.StateChanged += s => states.Add(s);

        await service.ConnectAsync("http://localhost:65335");

        states.Should().ContainInOrder(ConnectionState.Connecting, ConnectionState.Connected);
    }

    [Fact]
    public async Task RunAgentAsync_WithoutConnect_ShouldThrow()
    {
        using var service = new KernelClientService();

        await FluentActions
            .Invoking(() => service.RunAgentAsync("test"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Not connected*");
    }

    [Fact]
    public async Task SearchMemoryAsync_WithoutConnect_ShouldThrow()
    {
        using var service = new KernelClientService();

        await FluentActions
            .Invoking(() => service.SearchMemoryAsync("query"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Not connected*");
    }

    [Fact]
    public async Task CheckHealthAsync_WithoutConnect_ShouldThrow()
    {
        using var service = new KernelClientService();

        await FluentActions
            .Invoking(() => service.CheckHealthAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Not connected*");
    }

    [Fact]
    public async Task GetEmotionalMoodAsync_WithoutConnect_ShouldReturnNull()
    {
        using var service = new KernelClientService();
        var mood = await service.GetEmotionalMoodAsync();
        mood.Should().BeNull();
    }

    [Fact]
    public async Task GetEmotionalMoodAsync_WithLowValence_ShouldReturnMood()
    {
        using var handler = new MockHttpHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath == "/health")
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"ok\": true, \"ts\": \"2026-01-01T00:00:00Z\"}"),
                };
            if (req.RequestUri?.AbsolutePath == "/profile/emotional")
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"valence\": -0.6, \"arousal\": 0.8, \"motivation\": 0.3, \"updatedAt\": \"2026-05-13T12:00:00Z\"}"),
                };
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler);
        using var service = new KernelClientService(http);

        await service.ConnectAsync("http://localhost:65335");
        var mood = await service.GetEmotionalMoodAsync();

        mood.Should().Be("😰 Tenso");
    }

    [Fact]
    public async Task GetEmotionalMoodAsync_WithServerError_ShouldReturnNull()
    {
        using var handler = new MockHttpHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath == "/health")
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"ok\": true, \"ts\": \"2026-01-01T00:00:00Z\"}"),
                };
            if (req.RequestUri?.AbsolutePath == "/profile/emotional")
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler);
        using var service = new KernelClientService(http);

        await service.ConnectAsync("http://localhost:65335");
        var mood = await service.GetEmotionalMoodAsync();

        mood.Should().BeNull();
    }

    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
