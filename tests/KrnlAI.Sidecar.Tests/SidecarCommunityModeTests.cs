using Microsoft.AspNetCore.Mvc.Testing;

namespace KrnlAI.Sidecar.Tests;

public sealed class SidecarCommunityModeTests(CommunitySidecarWebAppFactory factory) : IClassFixture<CommunitySidecarWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_CommunityMode_ShouldReportCommunity()
    {
        var response = await _client.GetFromJsonAsync<Dictionary<string, object>>("/health", TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response!["mode"].ToString().Should().Be("community");
    }

    [Fact]
    public async Task AgentRun_CommunityMode_ShouldUseEmbeddedKernel()
    {
        var response = await _client.PostAsJsonAsync("/agent/run", new { prompt = "hello local" }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AgentRunResponse>(TestContext.Current.CancellationToken);

        body.Should().NotBeNull();
        body!.Narration.Should().Contain("hello local");
    }

    [Theory]
    [InlineData("/policy/list")]
    [InlineData("/memory/metrics")]
    [InlineData("/episodes/search")]
    [InlineData("/agent/metrics/scorecard")]
    [InlineData("/observability/runtime/summary")]
    [InlineData("/goals/active")]
    [InlineData("/cognitive/dashboard")]
    [InlineData("/benchmark/summary")]
    [InlineData("/versions")]
    [InlineData("/versions/contracts")]
    [InlineData("/archive/stats")]
    [InlineData("/snapshots")]
    [InlineData("/objectives")]
    [InlineData("/objectives/active")]
    [InlineData("/investigations")]
    [InlineData("/api/documents?limit=50")]
    public async Task ReadEndpoints_CommunityMode_ShouldReturnStandaloneFallback(string path)
    {
        var response = await _client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}

public sealed class CommunitySidecarWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Sidecar:Mode", "Community");
        builder.UseSetting("Store:Mode", "InMemory");
        builder.UseSetting("Vector:Mode", "InMemory");
        builder.UseSetting("Cache:Mode", "Memory");
    }
}
