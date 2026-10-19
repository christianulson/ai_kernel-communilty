using System.Net;
using System.Net.Http;
using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

public sealed class BacklogServiceTests
{
    [Fact]
    public async Task GetItemsAsync_WithApiResponse_ShouldReturnItems()
    {
        using var handler = new MockHttpHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath == "/api/backlog")
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """[{"id":"B-001","title":"Task 1","description":"","status":"Pending","priority":"High","dependencies":[],"tags":[],"createdAt":"2026-07-15T12:00:00Z","updatedAt":"2026-07-15T12:00:00Z"}]"""),
                };
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        using var service = new BacklogService("http://localhost", http);

        var result = await service.GetItemsAsync(null, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].Id.Should().Be("B-001");
        result[0].Title.Should().Be("Task 1");
        result[0].Status.Should().Be("Pending");
        result[0].Priority.Should().Be("High");
    }

    [Fact]
    public async Task GetItemsAsync_WithStatusFilter_ShouldPassQueryParam()
    {
        using var handler = new MockHttpHandler(req =>
        {
            req.RequestUri?.Query.Should().Contain("status=InProgress");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]"),
            };
        });

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        using var service = new BacklogService("http://localhost", http);

        var result = await service.GetItemsAsync("InProgress", CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetItemsAsync_WithServerError_ShouldReturnNull()
    {
        using var handler = new MockHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        using var service = new BacklogService("http://localhost", http);

        var result = await service.GetItemsAsync(null, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateItemAsync_WithValidData_ShouldReturnCreatedItem()
    {
        using var handler = new MockHttpHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath == "/api/backlog" && req.Method == HttpMethod.Post)
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(
                        """{"id":"B-002","title":"New Task","description":"Desc","status":"Pending","priority":"High","dependencies":[],"tags":["bug"],"createdAt":"2026-07-15T12:00:00Z","updatedAt":"2026-07-15T12:00:00Z"}"""),
                };
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        using var service = new BacklogService("http://localhost", http);

        var result = await service.CreateItemAsync("New Task", "Desc", "High", ["bug"], CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be("B-002");
        result.Title.Should().Be("New Task");
    }

    [Fact]
    public async Task CreateItemAsync_WithEmptyTitle_ShouldReturnNull()
    {
        using var handler = new MockHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest));

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        using var service = new BacklogService("http://localhost", http);

        var result = await service.CreateItemAsync("", "Desc", "High", null, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task MoveItemAsync_WithSuccess_ShouldReturnTrue()
    {
        using var handler = new MockHttpHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath == "/api/backlog/B-001/status" && req.Method == new HttpMethod("PATCH"))
                return new HttpResponseMessage(HttpStatusCode.OK);
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        using var service = new BacklogService("http://localhost", http);

        var result = await service.MoveItemAsync("B-001", "InProgress", CancellationToken.None);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task MoveItemAsync_WithError_ShouldReturnFalse()
    {
        using var handler = new MockHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest));

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        using var service = new BacklogService("http://localhost", http);

        var result = await service.MoveItemAsync("B-001", "invalid", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteItemAsync_WithSuccess_ShouldReturnTrue()
    {
        using var handler = new MockHttpHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath == "/api/backlog/B-001" && req.Method == HttpMethod.Delete)
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        using var service = new BacklogService("http://localhost", http);

        var result = await service.DeleteItemAsync("B-001", CancellationToken.None);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteItemAsync_WithNotFound_ShouldReturnFalse()
    {
        using var handler = new MockHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound));

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        using var service = new BacklogService("http://localhost", http);

        var result = await service.DeleteItemAsync("B-999", CancellationToken.None);
        result.Should().BeFalse();
    }

    private sealed class MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(handler(request));
    }
}
