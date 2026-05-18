using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KrnlAI.Sidecar.Tests;

public sealed class SidecarCommunityModeTests : IClassFixture<CommunitySidecarWebAppFactory>
{
    private readonly HttpClient _client;

    public SidecarCommunityModeTests(CommunitySidecarWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

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
}

public sealed class CommunitySidecarWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Sidecar:Mode", "Community");
    }
}
