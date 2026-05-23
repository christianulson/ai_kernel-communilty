using KrnlAI.Embedded;
using KrnlAI.Sidecar.Rpc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KrnlAI.Sidecar.Tests;

public sealed class SidecarRpcHandlerTests
{
    [Fact]
    public async Task RunAgentAsync_WithValidPrompt_ShouldReturnNarration()
    {
        var kernel = new EmbeddedKrnlAI();
        var handler = new SidecarRpcHandler(kernel, NullLogger<SidecarRpcHandler>.Instance);

        var result = await handler.RunAgentAsync("hello", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains("hello", result.Narration ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task RunAgentAsync_WithNullPrompt_ShouldNotThrow()
    {
        var kernel = new EmbeddedKrnlAI();
        var handler = new SidecarRpcHandler(kernel, NullLogger<SidecarRpcHandler>.Instance);

        var result = await handler.RunAgentAsync(null!, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Narration);
    }

    [Fact]
    public async Task GetHealthAsync_ShouldReturnHealthy()
    {
        var kernel = new EmbeddedKrnlAI();
        var handler = new SidecarRpcHandler(kernel, NullLogger<SidecarRpcHandler>.Instance);

        var result = await handler.GetHealthAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("healthy", result.Status);
        Assert.Equal("rpc", result.Mode);
    }

    [Fact]
    public async Task SearchMemoryAsync_ShouldReturnHits()
    {
        var kernel = new EmbeddedKrnlAI();
        var handler = new SidecarRpcHandler(kernel, NullLogger<SidecarRpcHandler>.Instance);

        var result = await handler.SearchMemoryAsync("test", CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Hits);
    }
}
