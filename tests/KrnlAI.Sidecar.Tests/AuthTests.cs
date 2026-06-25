namespace KrnlAI.Sidecar.Tests;

public sealed class AuthTests_WithoutToken(SidecarWebAppFactory factory) : IClassFixture<SidecarWebAppFactory>
{
    private readonly HttpClient _http = factory.CreateClient();

    [Fact]
    public async Task Health_ShouldBeAccessible_WithoutAuth()
    {
        var res = await _http.GetAsync("/health", TestContext.Current.CancellationToken).ConfigureAwait(false);
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task AgentRun_ShouldBeAccessible_WhenNoTokenConfigured()
    {
        var payload = new { prompt = "hello" };
        var res = await _http.PostAsJsonAsync("/agent/run", payload, TestContext.Current.CancellationToken).ConfigureAwait(false);
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}

public sealed class AuthTests_WithToken(AuthSidecarWebAppFactory factory) : IClassFixture<AuthSidecarWebAppFactory>
{
    [Fact]
    public async Task AgentRun_ShouldReturn401_WhenAuthTokenMissing()
    {
        var http = factory.CreateClient();
        var payload = new { prompt = "hello" };
        var res = await http.PostAsJsonAsync("/agent/run", payload, TestContext.Current.CancellationToken).ConfigureAwait(false);
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AgentRun_ShouldReturn200_WhenAuthTokenMatches()
    {
        var http = factory.CreateClient();
        http.DefaultRequestHeaders.Add("Authorization", "Bearer test-secret-123");
        var payload = new { prompt = "hello" };
        var res = await http.PostAsJsonAsync("/agent/run", payload, TestContext.Current.CancellationToken).ConfigureAwait(false);
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task AgentRun_ShouldReturn401_WhenAuthTokenWrong()
    {
        var http = factory.CreateClient();
        http.DefaultRequestHeaders.Add("Authorization", "Bearer wrong-token");
        var payload = new { prompt = "hello" };
        var res = await http.PostAsJsonAsync("/agent/run", payload, TestContext.Current.CancellationToken).ConfigureAwait(false);
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PolicyList_ShouldReturn401_WhenAuthRequired()
    {
        var http = factory.CreateClient();
        var res = await http.GetAsync("/policy/list", TestContext.Current.CancellationToken).ConfigureAwait(false);
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }
}

public sealed class AuthTests_Community_WithToken(AuthCommunitySidecarWebAppFactory factory) : IClassFixture<AuthCommunitySidecarWebAppFactory>
{
    [Fact]
    public async Task AgentRun_Community_ShouldReturn401_WhenAuthTokenMissing()
    {
        var http = factory.CreateClient();
        var payload = new { prompt = "hello" };
        var res = await http.PostAsJsonAsync("/agent/run", payload, TestContext.Current.CancellationToken).ConfigureAwait(false);
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AgentRun_Community_ShouldReturn200_WhenAuthTokenMatches()
    {
        var http = factory.CreateClient();
        http.DefaultRequestHeaders.Add("Authorization", "Bearer test-secret-123");
        var payload = new { prompt = "hello" };
        var res = await http.PostAsJsonAsync("/agent/run", payload, TestContext.Current.CancellationToken).ConfigureAwait(false);
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}