using System.Net;
using System.Net.Http;
using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

public sealed class PoliciesServiceTests
{
    [Fact]
    public async Task GetPoliciesAsync_WithApiResponse_ShouldReturnPolicies()
    {
        using var handler = new MockHttpHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath == "/policy/list")
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"policies":[{"id":"1","name":"Review before merge","description":"Require review","domain":"Safety","isActive":true,"score":0.95},{"id":"2","name":"Auto-format","description":"Auto format code","domain":"Code","isActive":false,"score":0.6}],"totalCount":2}"""),
                };
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler);
        using var service = new PoliciesService(http);

        var policies = await service.GetPoliciesAsync(CancellationToken.None);

        policies.Should().HaveCount(2);
        policies[0].Name.Should().Be("Review before merge");
        policies[0].Domain.Should().Be("Safety");
        policies[0].IsActive.Should().BeTrue();
        policies[1].Name.Should().Be("Auto-format");
        policies[1].IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetPoliciesAsync_WithServerError_ShouldReturnEmpty()
    {
        using var handler = new MockHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        using var http = new HttpClient(handler);
        using var service = new PoliciesService(http);

        var policies = await service.GetPoliciesAsync(CancellationToken.None);
        policies.Should().BeEmpty();
    }

    [Fact]
    public async Task TogglePolicyAsync_ShouldReturnTrueOnSuccess()
    {
        using var handler = new MockHttpHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath == "/policy/p1" && req.Method == HttpMethod.Put)
                return new HttpResponseMessage(HttpStatusCode.OK);
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler);
        using var service = new PoliciesService(http);

        var result = await service.TogglePolicyAsync("p1", true, CancellationToken.None);
        result.Should().BeTrue();
    }

    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
        public MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handler = handler;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(_handler(request));
    }
}
