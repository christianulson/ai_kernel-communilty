using System.Net;
using FluentAssertions;
using KrnlAI.VisualStudio.Extensibility.Core.Services;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests.Services;

public sealed class KernelClientServiceTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public FakeHandler(HttpStatusCode statusCode, string content)
        {
            _response = new HttpResponseMessage(statusCode) { Content = new StringContent(content) };
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }

    [Fact]
    public async Task ConnectAsync_HealthyEndpoint_ShouldTransitionToConnected()
    {
        var http = new HttpClient(new FakeHandler(HttpStatusCode.OK, """{"ok":true,"ts":"2026-01-01"}"""));
        using var client = new KernelClientService(http: http);

        var connected = await client.ConnectAsync("http://localhost:5235");

        connected.Should().BeTrue();
        client.State.Should().Be(ConnectionState.Connected);
        client.BaseUrl.Should().Be("http://localhost:5235");
    }

    [Fact]
    public async Task ConnectAsync_UnhealthyEndpoint_ShouldTransitionToFailed()
    {
        var http = new HttpClient(new FakeHandler(HttpStatusCode.InternalServerError, "boom"));
        using var client = new KernelClientService(http: http, maxRetries: 2);

        var connected = await client.ConnectAsync("http://localhost:5235");

        connected.Should().BeFalse();
        client.State.Should().Be(ConnectionState.Failed);
    }

    [Fact]
    public async Task RunAgentAsync_WithoutConnection_ShouldThrow()
    {
        using var client = new KernelClientService();

        var act = async () => await client.RunAgentAsync("goal");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Not connected*");
    }
}