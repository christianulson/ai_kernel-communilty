using System.Net;
using KrnlAI.Desktop.App.Services;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class HttpSlashCommandExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_Clear_ShouldReturnConstant()
    {
        var executor = new HttpSlashCommandExecutor("http://localhost");
        var result = await executor.ExecuteAsync("/clear");
        Assert.Equal("CLEAR_CONVERSATION", result);
    }

    [Fact]
    public async Task ExecuteAsync_Help_ShouldReturnHelpText()
    {
        var executor = new HttpSlashCommandExecutor("http://localhost");
        var result = await executor.ExecuteAsync("/help");
        Assert.Contains("/undo", result);
        Assert.Contains("/diff", result);
    }

    [Fact]
    public async Task ExecuteAsync_Unknown_ShouldReturnError()
    {
        var executor = new HttpSlashCommandExecutor("http://localhost");
        var result = await executor.ExecuteAsync("/nonexistent");
        Assert.Contains("Error", result);
    }

    [Fact]
    public async Task ExecuteAsync_ApiFailure_ShouldReturnError()
    {
        var handler = new MockHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var executor = new HttpSlashCommandExecutor("http://localhost");

        var result = await executor.ExecuteAsync("/undo");

        Assert.Contains("Error", result);
    }

    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
        public MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handler = handler;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(_handler(request));
    }
}
