using Microsoft.AspNetCore.Mvc.Testing;

namespace KrnlAI.Sidecar.Tests;

public class SidecarWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("https_port", "5001");
    }
}

public sealed class AuthSidecarWebAppFactory : SidecarWebAppFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Sidecar:Auth:Token", "test-secret-123");
    }
}

public sealed class AuthCommunitySidecarWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Sidecar:Mode", "Community");
        builder.UseSetting("Sidecar:Auth:Token", "test-secret-123");
        builder.UseSetting("Store:Mode", "InMemory");
        builder.UseSetting("Vector:Mode", "InMemory");
        builder.UseSetting("Cache:Mode", "Memory");
    }
}
