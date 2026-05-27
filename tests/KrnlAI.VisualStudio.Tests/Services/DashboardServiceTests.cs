using System.Net;
using System.Net.Http;
using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

public sealed class DashboardServiceTests
{
    [Fact]
    public async Task GetScorecardAsync_WithApiResponse_ShouldReturnScorecard()
    {
        using var handler = new MockHttpHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath == "/agent/metrics/scorecard")
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"reliability":0.8,"efficiency":0.6,"safety":0.9,"antiLoop":0.5,"governance":0.7,"overall":0.75}"""),
                };
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler);
        using var service = new DashboardService(http);

        var scorecard = await service.GetScorecardAsync(CancellationToken.None);

        scorecard.Should().NotBeNull();
        scorecard!.GoalAutonomy.Should().Be(0.8);
        scorecard.ExecutionAutonomy.Should().Be(0.6);
        scorecard.SafetyAutonomy.Should().Be(0.9);
        scorecard.LearningAutonomy.Should().Be(0.5);
        scorecard.MetaCognitionAutonomy.Should().Be(0.7);
    }

    [Fact]
    public async Task GetScorecardAsync_WithServerError_ShouldReturnNull()
    {
        using var handler = new MockHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        using var http = new HttpClient(handler);
        using var service = new DashboardService(http);

        var scorecard = await service.GetScorecardAsync(CancellationToken.None);
        scorecard.Should().BeNull();
    }

    [Fact]
    public async Task GetHealthAsync_WithHealthyEndpoint_ShouldReturnOk()
    {
        using var handler = new MockHttpHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath == "/health")
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"ok":true,"ts":"2026-01-01T00:00:00Z"}"""),
                };
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler);
        using var service = new DashboardService(http);

        var health = await service.GetHealthAsync(CancellationToken.None);

        health.Should().NotBeNull();
        health!.Status.Should().Be("OK");
    }

    [Fact]
    public async Task GetHealthAsync_WithError_ShouldReturnUnreachable()
    {
        using var handler = new MockHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        using var http = new HttpClient(handler);
        using var service = new DashboardService(http);

        var health = await service.GetHealthAsync(CancellationToken.None);
        health!.Status.Should().Be("Unreachable");
    }

    [Fact]
    public async Task GetMoodAsync_WithHighValence_ShouldReturnCalm()
    {
        using var handler = new MockHttpHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath == "/profile/emotional")
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"valence":0.8,"arousal":0.2,"motivation":0.5}"""),
                };
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler);
        using var service = new DashboardService(http);

        var mood = await service.GetMoodAsync(CancellationToken.None);
        mood.Should().Contain("Calm");
    }

    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
        public MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handler = handler;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(_handler(request));
    }
}
