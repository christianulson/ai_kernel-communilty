using System.Net;
using System.Net.Http;
using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.ToolWindows.QA;

public sealed class QAToolWindowTests
{
    [Fact]
    public async Task LoadRuns_WithApiResponse_ShouldReturnItems()
    {
        using var handler = new MockHttpHandler(req =>
        {
            req.RequestUri?.AbsolutePath.Should().Be("/api/backlog");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """[{"id":"B-001","title":"QA Smoke Test","description":"","status":"Pending","priority":"Medium","dependencies":[],"tags":[],"createdAt":"2026-07-15T12:00:00Z","updatedAt":"2026-07-15T12:00:00Z"}]"""),
            };
        });

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        using var service = new BacklogService("http://localhost", http);

        var items = await service.GetItemsAsync(null, CancellationToken.None);

        items.Should().NotBeNull();
        items.Should().HaveCount(1);
        items![0].Title.Should().Be("QA Smoke Test");
    }

    [Fact]
    public async Task StartTest_WithPost_ShouldCreateItem()
    {
        using var handler = new MockHttpHandler(req =>
        {
            if (req.Method == HttpMethod.Post && req.RequestUri?.AbsolutePath == "/api/backlog")
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(
                        """{"id":"B-002","title":"QA Test - Smoke","description":"Automated QA test: Smoke","status":"Pending","priority":"Medium","dependencies":[],"tags":[],"createdAt":"2026-07-15T12:00:00Z","updatedAt":"2026-07-15T12:00:00Z"}"""),
                };
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        using var service = new BacklogService("http://localhost", http);

        var item = await service.CreateItemAsync("QA Test - Smoke", "Automated QA test: Smoke", "Medium", null, CancellationToken.None);

        item.Should().NotBeNull();
        item!.Title.Should().Be("QA Test - Smoke");
    }

    [Fact]
    public async Task GetEvidence_WithExistingItem_ShouldReturnDetails()
    {
        using var handler = new MockHttpHandler(req =>
        {
            req.RequestUri?.AbsolutePath.Should().Be("/api/backlog");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """[{"id":"B-001","title":"QA Smoke Test","description":"","status":"Done","priority":"Medium","dependencies":[],"tags":[],"createdAt":"2026-07-15T12:00:00Z","updatedAt":"2026-07-15T12:00:00Z"}]"""),
            };
        });

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        using var service = new BacklogService("http://localhost", http);

        var items = await service.GetItemsAsync(null, CancellationToken.None);
        var item = items?.FirstOrDefault(i => i.Id == "B-001");

        item.Should().NotBeNull();
        item!.Status.Should().Be("Done");
    }

    private sealed class MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(handler(request));
    }
}
