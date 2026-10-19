using System.Net;
using System.Text;
using FluentAssertions;
using KrnlAI.Sdk;
using KrnlAI.Sdk.Models;
using Xunit;

namespace KrnlAISdk.Tests;

public sealed class KrnlAIClientTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = new();
        public List<string> RequestBodies { get; } = new();
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (request.Content is not null)
                RequestBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            return _responder(request);
        }

        public static HttpResponseMessage Json(HttpStatusCode code, string json)
            => new(code) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    [Fact]
    public async Task HealthCheckAsync_OkResponse_ShouldReturnHealthy()
    {
        var handler = new FakeHandler(_ => FakeHandler.Json(HttpStatusCode.OK, """{"ok":true,"ts":"2026-01-01T00:00:00Z"}"""));
        using var client = new KrnlAIClient("http://localhost:5235", new HttpClient(handler));

        var health = await client.HealthCheckAsync();

        health.Ok.Should().BeTrue();
        health.Ts.Should().Be("2026-01-01T00:00:00Z");
        handler.Requests.Single().RequestUri.Should().Be("http://localhost:5235/health");
    }

    [Fact]
    public async Task HealthCheckAsync_BaseUrlWithTrailingSlash_ShouldNotDoubleSlash()
    {
        var handler = new FakeHandler(_ => FakeHandler.Json(HttpStatusCode.OK, """{"ok":true,"ts":"x"}"""));
        using var client = new KrnlAIClient("http://localhost:5235/", new HttpClient(handler));

        await client.HealthCheckAsync();

        handler.Requests.Single().RequestUri!.ToString().Should().Be("http://localhost:5235/health");
    }

    [Fact]
    public async Task AgentRunAsync_ShouldSendCamelCaseBodyAndParseResponse()
    {
        var handler = new FakeHandler(_ => FakeHandler.Json(HttpStatusCode.OK, """
            {"goal":"fix bug","status":"completed","summary":"done","steps":[]}
            """));
        using var client = new KrnlAIClient("http://localhost:5235", new HttpClient(handler));

        var response = await client.AgentRunAsync(new AgentRunRequest(Goal: "fix bug", MaxSteps: 5));

        response.Goal.Should().Be("fix bug");
        response.Status.Should().Be("completed");
        handler.Requests.Single().Method.Should().Be(HttpMethod.Post);
        handler.Requests.Single().RequestUri.Should().Be("http://localhost:5235/agent/run");

        var body = handler.RequestBodies.Single();
        body.Should().Contain("\"goal\":\"fix bug\"");
        body.Should().Contain("\"maxSteps\":5");
    }

    [Fact]
    public async Task GoalsListAsync_ShouldParseArray()
    {
        var handler = new FakeHandler(_ => FakeHandler.Json(HttpStatusCode.OK, """[{"goalId":"g1","goal":"a"}]"""));
        using var client = new KrnlAIClient("http://localhost:5235", new HttpClient(handler));

        var goals = await client.GoalsListAsync();

        goals.Should().HaveCount(1);
        handler.Requests.Single().RequestUri.Should().Be("http://localhost:5235/goals/active");
    }

    [Fact]
    public async Task Request_Unauthorized_ShouldThrowAuthenticationException()
    {
        var handler = new FakeHandler(_ => FakeHandler.Json(HttpStatusCode.Unauthorized, "{}"));
        using var client = new KrnlAIClient("http://localhost:5235", new HttpClient(handler));

        var act = async () => await client.HealthCheckAsync();

        await act.Should().ThrowAsync<KrnlAIAuthenticationException>();
    }

    [Fact]
    public async Task Request_RateLimited_ShouldThrowRateLimitException()
    {
        var handler = new FakeHandler(_ => FakeHandler.Json((HttpStatusCode)429, "{}"));
        using var client = new KrnlAIClient("http://localhost:5235", new HttpClient(handler));

        var act = async () => await client.HealthCheckAsync();

        await act.Should().ThrowAsync<KrnlAIRateLimitException>();
    }

    [Fact]
    public async Task Request_ValidationError_ShouldThrowWithStatusCode400()
    {
        var handler = new FakeHandler(_ => FakeHandler.Json(HttpStatusCode.BadRequest, """{"error":"bad"}"""));
        using var client = new KrnlAIClient("http://localhost:5235", new HttpClient(handler));

        var act = async () => await client.HealthCheckAsync();

        var ex = await act.Should().ThrowAsync<KrnlAIValidationException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.Message.Should().Contain("bad");
    }

    [Fact]
    public async Task Request_ServerError_ShouldThrowServerException()
    {
        var handler = new FakeHandler(_ => FakeHandler.Json(HttpStatusCode.InternalServerError, "boom"));
        using var client = new KrnlAIClient("http://localhost:5235", new HttpClient(handler));

        var act = async () => await client.HealthCheckAsync();

        var ex = await act.Should().ThrowAsync<KrnlAIServerException>();
        ex.Which.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task Request_EmptySuccessBody_ShouldThrowKrnlAIException()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("")
        });
        using var client = new KrnlAIClient("http://localhost:5235", new HttpClient(handler));

        var act = async () => await client.HealthCheckAsync();

        await act.Should().ThrowAsync<KrnlAIException>();
    }

    [Fact]
    public async Task Request_EmptyFailureBody_ShouldStillMapStatusCode()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("")
        });
        using var client = new KrnlAIClient("http://localhost:5235", new HttpClient(handler));

        var act = async () => await client.HealthCheckAsync();

        await act.Should().ThrowAsync<KrnlAIServerException>();
    }
}