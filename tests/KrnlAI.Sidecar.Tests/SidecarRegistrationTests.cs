using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;

namespace KrnlAI.Sidecar.Tests;

[Trait("Category", "Unit")]
public sealed class SidecarRegistrationTests
{
    [Fact]
    public void AddSidecarServices_ShouldResolveSuccessfully()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Sidecar:Enterprise:Enabled"] = "false",
                ["Sidecar:KernelApi:BaseUrl"] = "http://localhost:5000",
            })
            .Build();

        services.AddSidecarServices(config, new TestHostEnvironment());

        var provider = services.BuildServiceProvider();
        var failures = new List<string>();

        foreach (var descriptor in services)
        {
            var type = descriptor.ServiceType;
            if (!type.IsInterface) continue;
            if (type.IsGenericType) continue;

            try
            {
                provider.GetRequiredService(type);
            }
            catch (Exception ex)
            {
                failures.Add($"{type.Name}: {ex.GetType().Name} — {ex.Message}");
            }
        }

        Assert.True(failures.Count == 0,
            $"{failures.Count} registro(s) do Sidecar falharam na resolução:\n  {string.Join("\n  ", failures)}");
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "KrnlAI.Sidecar.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider
        {
            get => new PhysicalFileProvider(ContentRootPath);
            set { }
        }
    }
}
