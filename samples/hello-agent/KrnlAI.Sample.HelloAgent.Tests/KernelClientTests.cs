using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace KrnlAI.Sample.HelloAgent.Tests;

public sealed class KernelClientTests
{
    [Fact]
    public async Task KernelClient_RunAgentAsync_ValidResponse_ShouldReturnResult()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new FakeHttpHandler(req =>
        {
            Assert.Equal("/agent/run", req.RequestUri?.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    status = "completed",
                    summary = "Hello! I'm a Krnl-AI agent.",
                    steps = new[] { new { tool = "kernel.handle", success = true, error = (string?)null } }
                })
            };
        });

        var client = new KernelClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") });
        var result = await client.RunAgentAsync("Say hello", ct);

        Assert.Equal("completed", result.Status);
        Assert.Contains("Hello", result.Summary);
        Assert.Single(result.Steps!);
        Assert.True(result.Steps![0].Success);
    }

    [Fact]
    public async Task KernelClient_RunAgentAsync_ServerError_ShouldThrow()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new FakeHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var client = new KernelClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") });

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.RunAgentAsync("test", ct));
    }

    private sealed class FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(handler(request));
    }
}
