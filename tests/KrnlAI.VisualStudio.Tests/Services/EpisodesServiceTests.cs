using System.Net;
using System.Net.Http;
using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

public sealed class EpisodesServiceTests
{
    [Fact]
    public async Task GetEpisodesAsync_WithApiResponse_ShouldReturnEpisodes()
    {
        using var handler = new MockHttpHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath == "/episodes/search")
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"episodes":[{"id":"e1","goalId":"Refactor module","status":"Completed","createdAt":"2026-01-01T12:00:00","durationMs":150000},{"id":"e2","goalId":"Fix bug","status":"Failed","createdAt":"2026-01-01T13:00:00","durationMs":60000}],"totalCount":2}"""),
                };
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler);
        using var service = new EpisodesService(http);

        var episodes = await service.GetEpisodesAsync(CancellationToken.None);

        episodes.Should().HaveCount(2);
        episodes[0].Goal.Should().Be("Refactor module");
        episodes[0].Status.Should().Be("Completed");
        episodes[1].Goal.Should().Be("Fix bug");
        episodes[1].Status.Should().Be("Failed");
    }

    [Fact]
    public async Task GetEpisodesAsync_WithServerError_ShouldReturnEmpty()
    {
        using var handler = new MockHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        using var http = new HttpClient(handler);
        using var service = new EpisodesService(http);

        var episodes = await service.GetEpisodesAsync(CancellationToken.None);
        episodes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEpisodeDetailsAsync_WithDetails_ShouldReturnEpisode()
    {
        using var handler = new MockHttpHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath == "/episodes/e1")
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"id":"e1","goal":"Refactor","status":"Completed","timestamp":"2026-01-01T12:00:00","stepCount":3,"steps":[{"number":1,"tool":"analyze","result":"OK","success":true},{"number":2,"tool":"edit","result":"OK","success":true}]}"""),
                };
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler);
        using var service = new EpisodesService(http);

        var episode = await service.GetEpisodeDetailsAsync("e1", CancellationToken.None);

        episode.Should().NotBeNull();
        episode!.Goal.Should().Be("Refactor");
        episode.Steps.Should().HaveCount(2);
        episode.Steps![0].Tool.Should().Be("analyze");
    }

    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
        public MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handler = handler;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(_handler(request));
    }
}
