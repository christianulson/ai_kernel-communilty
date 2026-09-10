using KrnlAI.Desktop.Core.Abstractions;

namespace KrnlAI.Desktop.Tests.Abstractions;

public sealed class ApiEndpointResolverTests
{
    [Fact]
    public void Resolve_NoEnvNoConfigured_ShouldReturnDefaultGateway()
    {
        Environment.SetEnvironmentVariable("KRNL__API_BASE_URL", null);

        Assert.Equal("http://localhost:5235", ApiEndpointResolver.Resolve(null));
    }

    [Fact]
    public void Resolve_ConfiguredValue_ShouldBeUsedWhenNoEnv()
    {
        Environment.SetEnvironmentVariable("KRNL__API_BASE_URL", null);

        Assert.Equal("http://localhost:6000", ApiEndpointResolver.Resolve("http://localhost:6000"));
    }

    [Fact]
    public void Resolve_EnvValue_ShouldWinOverConfigured()
    {
        Environment.SetEnvironmentVariable("KRNL__API_BASE_URL", "http://localhost:7000");
        try
        {
            Assert.Equal("http://localhost:7000", ApiEndpointResolver.Resolve("http://localhost:6000"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("KRNL__API_BASE_URL", null);
        }
    }

    [Fact]
    public void Resolve_EnvValue_ShouldTrimTrailingSlash()
    {
        Environment.SetEnvironmentVariable("KRNL__API_BASE_URL", "http://localhost:7000/");
        try
        {
            Assert.Equal("http://localhost:7000", ApiEndpointResolver.Resolve(null));
        }
        finally
        {
            Environment.SetEnvironmentVariable("KRNL__API_BASE_URL", null);
        }
    }

    [Fact]
    public void Resolve_InvalidConfigured_ShouldFallbackToDefault()
    {
        Environment.SetEnvironmentVariable("KRNL__API_BASE_URL", null);

        Assert.Equal("http://localhost:5235", ApiEndpointResolver.Resolve("not a uri"));
    }
}