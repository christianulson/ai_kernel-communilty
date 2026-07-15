using System.Net;
using System.Net.Http;
using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.ToolWindows.Backlog;

public sealed class BacklogToolWindowTests
{
    private static BacklogService CreateServiceWithHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var http = new HttpClient(new MockHttpHandler(handler)) { BaseAddress = new Uri("http://localhost") };
        return new BacklogService("http://localhost", http);
    }

    [Fact]
    public async Task LoadData_WithApiResponse_ShouldReturnItems()
    {
        using var service = CreateServiceWithHandler(req =>
        {
            req.RequestUri?.AbsolutePath.Should().Be("/api/backlog");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """[{"id":"B-001","title":"Task 1","description":"","status":"Pending","priority":"High","dependencies":[],"tags":[],"createdAt":"2026-07-15T12:00:00Z","updatedAt":"2026-07-15T12:00:00Z"}]"""),
            };
        });

        var items = await service.GetItemsAsync(null, CancellationToken.None);

        items.Should().NotBeNull();
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task MoveCard_WithPatch_ShouldReturnTrue()
    {
        using var service = CreateServiceWithHandler(req =>
        {
            req.Method.Should().Be(new HttpMethod("PATCH"));
            req.RequestUri?.AbsolutePath.Should().Be("/api/backlog/B-001/status");
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var result = await service.MoveItemAsync("B-001", "InProgress", CancellationToken.None);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CreateItem_WithPost_ShouldReturnCreatedItem()
    {
        using var service = CreateServiceWithHandler(req =>
        {
            req.Method.Should().Be(HttpMethod.Post);
            req.RequestUri?.AbsolutePath.Should().Be("/api/backlog");
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    """{"id":"B-002","title":"New Task","description":"Desc","status":"Pending","priority":"High","dependencies":[],"tags":[],"createdAt":"2026-07-15T12:00:00Z","updatedAt":"2026-07-15T12:00:00Z"}"""),
            };
        });

        var item = await service.CreateItemAsync("New Task", "Desc", "High", null, CancellationToken.None);
        item.Should().NotBeNull();
        item!.Id.Should().Be("B-002");
    }

    private sealed class MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(handler(request));
    }
}
