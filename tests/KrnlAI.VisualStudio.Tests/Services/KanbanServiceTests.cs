using System.Net;
using System.Net.Http;
using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

public sealed class KanbanServiceTests
{
    [Fact]
    public async Task GetKanbanAsync_WithApiResponse_ShouldReturnColumns()
    {
        using var handler = new MockHttpHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath == "/api/goals/kanban")
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"columns":[{"column":"backlog","label":"Backlog","cards":[{"id":"g1","description":"Setup","status":"active","progress":0,"priority":0.8,"domain":null,"createdAt":"2026-05-22T12:00:00Z","deadline":null,"parentGoalId":null,"subGoals":null}],"totalCount":1}],"metadata":{"totalGoals":1,"totalColumns":1,"filters":{"daysBack":10,"domain":null,"minPriority":null,"userId":null,"search":null}}}"""),
                };
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        using var service = new KanbanService("http://localhost", http);

        var result = await service.GetKanbanAsync(ct: CancellationToken.None);

        result.Should().NotBeNull();
        result!.Columns.Should().HaveCount(1);
        result.Columns[0].Column.Should().Be("backlog");
        result.Columns[0].Cards.Should().HaveCount(1);
        result.Columns[0].Cards[0].Description.Should().Be("Setup");
        result.Metadata.TotalGoals.Should().Be(1);
    }

    [Fact]
    public async Task GetKanbanAsync_WithServerError_ShouldReturnNull()
    {
        using var handler = new MockHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        using var service = new KanbanService("http://localhost", http);

        var result = await service.GetKanbanAsync(ct: CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task MoveCardAsync_WithSuccess_ShouldReturnTrue()
    {
        using var handler = new MockHttpHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath == "/api/goals/g1/status" && req.Method == new HttpMethod("PATCH"))
                return new HttpResponseMessage(HttpStatusCode.OK);
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        using var service = new KanbanService("http://localhost", http);

        var result = await service.MoveCardAsync("g1", "in_progress", CancellationToken.None);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task MoveCardAsync_WithError_ShouldReturnFalse()
    {
        using var handler = new MockHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest));

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        using var service = new KanbanService("http://localhost", http);

        var result = await service.MoveCardAsync("g1", "invalid", CancellationToken.None);
        result.Should().BeFalse();
    }

    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
        public MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handler = handler;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(_handler(request));
    }
}
