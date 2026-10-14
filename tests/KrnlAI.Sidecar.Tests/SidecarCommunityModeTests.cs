using KrnlAI.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json;

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
    public async Task CognitiveDashboard_CommunityMode_ShouldReturnRealKernelData()
    {
        var response = await _client.GetFromJsonAsync<JsonElement>("/cognitive/dashboard", TestContext.Current.CancellationToken);

        response.GetProperty("overallHealth").GetDouble().Should().BeGreaterThanOrEqualTo(0);
        response.GetProperty("activeModules").GetArrayLength().Should().BeGreaterThan(0);
        response.GetProperty("agiAsiLevel").GetString().Should().Contain("AgiL");
    }

    [Fact]
    public async Task BenchmarkSummary_CommunityMode_ShouldReturnRealEvalSuite()
    {
        var response = await _client.GetFromJsonAsync<JsonElement>("/benchmark/summary", TestContext.Current.CancellationToken);

        response.GetProperty("totalSuites").GetInt32().Should().BeGreaterThan(0);
        response.GetProperty("suites").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RuntimeSummary_CommunityMode_ShouldReportRealMemory()
    {
        var response = await _client.GetFromJsonAsync<JsonElement>("/observability/runtime/summary", TestContext.Current.CancellationToken);

        response.GetProperty("memoryUsageBytes").GetInt64().Should().BeGreaterThan(0);
        response.GetProperty("services").GetProperty("provider").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task WorkingMemory_CommunityMode_ShouldReturnRealSnapshot()
    {
        var response = await _client.GetFromJsonAsync<JsonElement>("/memory/working", TestContext.Current.CancellationToken);

        response.GetProperty("maxSlots").GetInt32().Should().BeGreaterThan(0);
        response.GetProperty("slots").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task Snapshots_CommunityMode_ShouldReturnArray()
    {
        var response = await _client.GetFromJsonAsync<JsonElement>("/snapshots", TestContext.Current.CancellationToken);

        response.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task Documents_CommunityMode_ShouldReturnArray()
    {
        var response = await _client.GetFromJsonAsync<JsonElement>("/api/documents?limit=10", TestContext.Current.CancellationToken);

        response.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task AgentRun_CommunityMode_ShouldUseEmbeddedKernel()
    {
        var response = await _client.PostAsJsonAsync("/agent/run", new { prompt = "hello local" }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AgentRunTransportResponse>(TestContext.Current.CancellationToken);

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
