using KrnlAI.Embedded.Abstractions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KrnlAI.Sidecar.Tests;

public class SidecarWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("https_port", "5001");
        ReplaceEmbeddedKernelWithFake(builder);
    }

    internal static void ReplaceEmbeddedKernelWithFake(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmbeddedKrnlAI));
            if (descriptor is not null)
                services.Remove(descriptor);
            services.AddSingleton<IEmbeddedKrnlAI>(new FakeEmbeddedKrnlAI());
        });
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
        SidecarWebAppFactory.ReplaceEmbeddedKernelWithFake(builder);
    }
}
