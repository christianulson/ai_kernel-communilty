namespace KrnlAI.Sidecar.Tests;

public sealed class MemoryAndEpisodesEndpointTests(SidecarWebAppFactory factory) : IClassFixture<SidecarWebAppFactory>
{
    private readonly HttpClient _http = factory.CreateClient();

    [Fact]
    public async Task MemorySearch_ShouldReturnEmptyHits()
    {
        var payload = new { query = "test", limit = 10 };
        var res = await _http.PostAsJsonAsync("/memory/search", payload, TestContext.Current.CancellationToken);

        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!["totalCount"].ToString().Should().Be("0");
    }

    [Fact]
    public async Task MemoryMetrics_ShouldReturnZeros()
    {
        var res = await _http.GetAsync("/memory/metrics", TestContext.Current.CancellationToken);

        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<Dictionary<string, int>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!["totalChunks"].Should().Be(0);
        body["totalDocuments"].Should().Be(0);
        body["totalSizeBytes"].Should().Be(0);
    }

    [Fact]
    public async Task EpisodesSearch_ShouldReturnEmpty()
    {
        var res = await _http.GetAsync("/episodes/search", TestContext.Current.CancellationToken);

        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!["totalCount"].ToString().Should().Be("0");
    }

    [Fact]
    public async Task EpisodesGetById_ShouldReturnEpisode()
    {
        var res = await _http.GetAsync("/episodes/test-123", TestContext.Current.CancellationToken);

        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!["id"].ToString().Should().Be("test-123");
        body["goalId"].ToString().Should().Be("standalone");
        body["status"].ToString().Should().Be("idle");
    }
}
