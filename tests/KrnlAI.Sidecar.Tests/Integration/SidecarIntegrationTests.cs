using FluentAssertions;
using KrnlAI.Embedded.Abstractions;
using KrnlAI.Sidecar;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace KrnlAI.Sidecar.Tests.Integration;

public sealed class SidecarIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SidecarIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> CreateMemoryFactory()
        => _factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Sidecar:Mode", "Community");
            b.UseSetting("Store:Mode", "Memory");
            b.UseSetting("Cache:Mode", "Memory");
            b.UseSetting("Vector:Mode", "Memory");
            b.UseSetting("LLM:Provider", "ollama");
        });

    [Fact]
    public void CommunityServices_ShouldRegisterEmbeddedKernel()
    {
        using var factory = CreateMemoryFactory();

        var kernel = factory.Services.GetService(typeof(IEmbeddedKrnlAI));

        kernel.Should().NotBeNull("AddSidecarCommunityServices deve registrar IEmbeddedKrnlAI");
    }

    [Fact]
    public async Task HealthEndpoint_ShouldRespondOk()
    {
        using var factory = CreateMemoryFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task PolicyListEndpoint_ShouldRespondOk()
    {
        using var factory = CreateMemoryFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/policy/list");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task UnknownRoute_ShouldReturn404()
    {
        using var factory = CreateMemoryFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/not-a-real-route");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
}