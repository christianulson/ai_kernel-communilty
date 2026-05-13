using System.Net.Http.Json;
using FluentAssertions;

namespace AIKernel.Sidecar.Tests;

public sealed class PolicyAndMetricsEndpointTests(SidecarWebAppFactory factory) : IClassFixture<SidecarWebAppFactory>
{
    private readonly HttpClient _http = factory.CreateClient();

    [Fact]
    public async Task PolicyList_ShouldReturnPolicies()
    {
        var res = await _http.GetAsync("/policy/list", TestContext.Current.CancellationToken);

        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.ContainsKey("policies").Should().BeTrue();
        body.ContainsKey("totalCount").Should().BeTrue();
    }

    [Fact]
    public async Task Scorecard_ShouldReturnMetrics()
    {
        var res = await _http.GetAsync("/agent/metrics/scorecard", TestContext.Current.CancellationToken);

        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<Dictionary<string, double>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Should().ContainKeys("reliability", "efficiency", "safety", "antiLoop", "governance", "overall");
        body["overall"].Should().BeGreaterThan(0);
    }
}
