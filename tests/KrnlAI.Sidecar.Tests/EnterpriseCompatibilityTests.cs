namespace KrnlAI.Sidecar.Tests;

public sealed class EnterpriseCompatibilityTests(SidecarWebAppFactory factory) : IClassFixture<SidecarWebAppFactory>
{
    [Fact]
    public async Task CommunityMode_DefaultConfig_ShouldReportCommunity()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/sidecar/diagnostics", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        body!["mode"].ToString().Should().Be("community");
    }

    [Fact]
    public async Task CommunityMode_HealthEndpoint_ShouldStillWork()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task EnterpriseMode_DiagnosticsEndpoint_ShouldReportEnterpriseConfig()
    {
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices((ctx, services) =>
            {
                services.Configure<SidecarOptions>(o =>
                {
                    o.Enterprise = new EnterpriseOptions
                    {
                        Enabled = true,
                        AuthEndpoint = "https://auth.example.com",
                        GatewayEndpoint = "https://gateway.example.com",
                        ApiKey = "test-key-123",
                        TenantId = "tenant-acme"
                    };
                });
            });
        }).CreateClient();

        var response = await client.GetAsync("/sidecar/diagnostics", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        body!["mode"].ToString().Should().Be("enterprise");
        body.Should().ContainKey("enterprise");
    }

    [Fact]
    public async Task EnterpriseMode_HealthEndpoint_ShouldStillWork()
    {
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices((ctx, services) =>
            {
                services.Configure<SidecarOptions>(o =>
                {
                    o.Enterprise = new EnterpriseOptions
                    {
                        Enabled = true,
                        AuthEndpoint = "https://auth.corp.com",
                        TenantId = "corp-1",
                        ApiKey = "secret"
                    };
                });
            });
        }).CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public void SidecarOptions_DefaultMode_ShouldBeCommunity()
    {
        var options = new SidecarOptions();
        options.EffectiveMode.Should().Be("community");
        options.Enterprise.Enabled.Should().BeFalse();
    }

    [Fact]
    public void SidecarOptions_EnterpriseEnabled_ShouldReportEnterpriseMode()
    {
        var options = new SidecarOptions
        {
            Enterprise = new EnterpriseOptions
            {
                Enabled = true,
                AuthEndpoint = "https://auth.example.com"
            }
        };
        options.EffectiveMode.Should().Be("enterprise");
    }
}
