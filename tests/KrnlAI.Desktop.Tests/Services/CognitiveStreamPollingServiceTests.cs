using KrnlAI.Desktop.App.Services;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class CognitiveStreamPollingServiceTests
{
    [Fact]
    public async Task ConnectAsync_WithCycleId_ShouldReadCanonicalSseEndpoint()
    {
        var handler = new CapturingHandler();
        var sut = new CognitiveStreamPollingService("http://localhost:5000", handler);

        await sut.ConnectAsync("cycle-123", CancellationToken.None).ConfigureAwait(false);
        await Task.Delay(500).ConfigureAwait(false);
        sut.Disconnect();

        Assert.Equal("/api/cognitive/stream/cycle-123", handler.LastPath);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? LastPath { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastPath = request.RequestUri?.AbsolutePath;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("")
            });
        }
    }
}
