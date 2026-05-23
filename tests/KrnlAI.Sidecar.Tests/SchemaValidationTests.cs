using Xunit;

namespace KrnlAI.Sidecar.Tests;

public sealed class SchemaValidationTests(SidecarWebAppFactory factory) : IClassFixture<SidecarWebAppFactory>
{
    private readonly HttpClient _http = factory.CreateClient();

    [Fact]
    public async Task MemorySearch_WithUnexpectedFields_ShouldReturn400()
    {
        var payload = new { query = "test", extraField = "hack" };
        var res = await _http.PostAsJsonAsync("/memory/search", payload, TestContext.Current.CancellationToken);

        res.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var body = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().ContainKey("error");
    }

    [Fact]
    public async Task MemorySearch_WithEmptyBody_ShouldReturn200WithEmptyHits()
    {
        var payload = new { };
        var res = await _http.PostAsJsonAsync("/memory/search", payload, TestContext.Current.CancellationToken);

        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task MemorySearch_WithNullBody_ShouldReturn400()
    {
        var res = await _http.PostAsync("/memory/search", content: null, cancellationToken: TestContext.Current.CancellationToken);

        res.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MemorySearch_WithValidQuery_ShouldReturn200()
    {
        var payload = new { query = "test" };
        var res = await _http.PostAsJsonAsync("/memory/search", payload, TestContext.Current.CancellationToken);

        var body = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().ContainKey("hits");
    }
}
