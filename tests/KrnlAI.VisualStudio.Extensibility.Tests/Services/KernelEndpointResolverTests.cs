using FluentAssertions;
using KrnlAI.VisualStudio.Extensibility.Core.Services;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests.Services;

public sealed class KernelEndpointResolverTests
{
    [Fact]
    public void Resolve_EmbeddedMode_ShouldReturnSidecarPort()
    {
        KernelEndpointResolver.Resolve(KernelRuntimeMode.Embedded, null, 5010)
            .Should().Be("http://127.0.0.1:5010");
    }

    [Fact]
    public void Resolve_EmbeddedMode_InvalidPort_ShouldFallbackTo5001()
    {
        KernelEndpointResolver.Resolve(KernelRuntimeMode.Embedded, null, 0)
            .Should().Be("http://127.0.0.1:5001");
    }

    [Fact]
    public void Resolve_LocalApi_NoEndpoint_ShouldReturnDefaultLocal()
    {
        KernelEndpointResolver.Resolve(KernelRuntimeMode.LocalApi, null, 5001)
            .Should().Be("http://localhost:5235");
    }

    [Fact]
    public void Resolve_LocalApi_NonLoopbackEndpoint_ShouldReturnDefaultLocal()
    {
        KernelEndpointResolver.Resolve(KernelRuntimeMode.LocalApi, "https://api.krnlai.dev", 5001)
            .Should().Be("http://localhost:5235");
    }

    [Fact]
    public void Resolve_LocalApi_LoopbackEndpoint_ShouldReturnEndpoint()
    {
        KernelEndpointResolver.Resolve(KernelRuntimeMode.LocalApi, "http://127.0.0.1:5000/", 5001)
            .Should().Be("http://127.0.0.1:5000");
    }

    [Fact]
    public void Resolve_CloudMode_NoEndpoint_ShouldReturnCloudDefault()
    {
        KernelEndpointResolver.Resolve(KernelRuntimeMode.Cloud, null, 5001)
            .Should().Be("https://api.krnlai.dev");
    }

    [Fact]
    public void Resolve_InvalidUri_ShouldReturnFallback()
    {
        KernelEndpointResolver.Resolve(KernelRuntimeMode.Cloud, "not a uri", 5001)
            .Should().Be("https://api.krnlai.dev");
    }
}