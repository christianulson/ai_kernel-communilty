using System.Net;
using FluentAssertions;
using KrnlAI.Cli.Abstractions;
using KrnlAI.Cli.Services;
using Xunit;

namespace KrnlAI.Cli.Tests.Services;

public sealed class ExternalEvaluatorsTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }
        public string? LastBody { get; private set; }
        private readonly HttpResponseMessage _response;

        public CapturingHandler(HttpStatusCode statusCode, string content)
        {
            _response = new HttpResponseMessage(statusCode) { Content = new StringContent(content) };
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            if (request.Content is not null)
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);
            return _response;
        }
    }

    [Fact]
    public async Task OpenAiEvaluator_WithApiKey_ShouldCallConfiguredBaseUrl()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, """{"results":[{"flagged":false}]}""");
        var http = new HttpClient(handler);
        using var evaluator = new OpenAiSafetyEvaluator(http, baseUrl: "http://localhost:7000");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key");
        try
        {
            var result = await evaluator.EvaluateAsync("hello", "s1");

            result.Blocked.Should().BeFalse();
            result.RiskLevel.Should().Be("low");
            handler.LastRequestUri.Should().Be("http://localhost:7000/v1/moderations");
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        }
    }

    [Fact]
    public async Task OpenAiEvaluator_NoApiKey_ShouldReturnSkipped()
    {
        var http = new HttpClient(new CapturingHandler(HttpStatusCode.OK, "{}"));
        using var evaluator = new OpenAiSafetyEvaluator(http);
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);

        var result = await evaluator.EvaluateAsync("hello", "s1");

        result.Blocked.Should().BeFalse();
        result.RiskLevel.Should().Be("skipped");
    }

    [Fact]
    public async Task OpenAiEvaluator_ServerError_ShouldReturnError()
    {
        var http = new HttpClient(new CapturingHandler(HttpStatusCode.InternalServerError, "boom"));
        using var evaluator = new OpenAiSafetyEvaluator(http, baseUrl: "http://localhost:7000");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key");
        try
        {
            var result = await evaluator.EvaluateAsync("hello", "s1");

            result.RiskLevel.Should().Be("error");
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        }
    }

    [Fact]
    public async Task AnthropicEvaluator_ShouldUseConfiguredModelAndEndpoint()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, """{"content":[{"type":"text","text":"SAFE"}]}""");
        var http = new HttpClient(handler);
        using var evaluator = new AnthropicSafetyEvaluator(
            http,
            baseUrl: "http://localhost:7000",
            model: "claude-test-1");
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", "test-key");
        try
        {
            var result = await evaluator.EvaluateAsync("hello", "s1");

            result.Blocked.Should().BeFalse();
            handler.LastRequestUri.Should().Be("http://localhost:7000/v1/messages");
            handler.LastBody.Should().Contain("claude-test-1");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", null);
        }
    }
}