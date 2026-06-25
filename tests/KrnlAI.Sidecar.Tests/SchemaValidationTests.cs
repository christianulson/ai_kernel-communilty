namespace KrnlAI.Sidecar.Tests;

public sealed class SchemaValidationTests(SidecarWebAppFactory factory) : IClassFixture<SidecarWebAppFactory>
{
    private readonly HttpClient _http = factory.CreateClient();

    [Fact]
    public async Task MemorySearch_WithUnexpectedFields_ShouldReturn400()
    {
        var payload = new { query = "test", extraField = "hack" };
        var res = await _http.PostAsJsonAsync("/memory/search", payload, TestContext.Current.CancellationToken).ConfigureAwait(false);

        res.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var body = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(false);
        body.Should().ContainKey("error");
    }

    [Fact]
    public async Task MemorySearch_WithEmptyBody_ShouldReturn200WithEmptyHits()
    {
        var payload = new { };
        var res = await _http.PostAsJsonAsync("/memory/search", payload, TestContext.Current.CancellationToken).ConfigureAwait(false);

        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task MemorySearch_WithNullBody_ShouldReturn400()
    {
        var res = await _http.PostAsync("/memory/search", content: null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(false);

        res.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MemorySearch_WithValidQuery_ShouldReturn200()
    {
        var payload = new { query = "test" };
        var res = await _http.PostAsJsonAsync("/memory/search", payload, TestContext.Current.CancellationToken).ConfigureAwait(false);

        var body = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(false);
        body.Should().ContainKey("hits");
    }
}

public sealed class SchemaValidationTests_CommunityMode(CommunitySidecarWebAppFactory factory) : IClassFixture<CommunitySidecarWebAppFactory>
{
    private readonly HttpClient _http = factory.CreateClient();

    [Fact]
    public async Task MemorySearch_Community_WithUnexpectedFields_ShouldReturn400()
    {
        var payload = new { query = "test", extraField = "hack" };
        var res = await _http.PostAsJsonAsync("/memory/search", payload, TestContext.Current.CancellationToken).ConfigureAwait(false);

        res.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MemorySearch_Community_WithEmptyQuery_ShouldReturn400()
    {
        var payload = new { query = "" };
        var res = await _http.PostAsJsonAsync("/memory/search", payload, TestContext.Current.CancellationToken).ConfigureAwait(false);

        res.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MemorySearch_Community_WithValidQuery_ShouldReturn200()
    {
        var payload = new { query = "test" };
        var res = await _http.PostAsJsonAsync("/memory/search", payload, TestContext.Current.CancellationToken).ConfigureAwait(false);

        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}
