using Microsoft.AspNetCore.Mvc.Testing;

namespace AIKernel.Sidecar.Tests;

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
