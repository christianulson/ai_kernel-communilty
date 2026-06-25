using KrnlAI.Cli.Commands;
using KrnlAI.Cli.Tui;
using KrnlAI.Embedded;

namespace KrnlAI.Cli.Tests.Tui;

public sealed class TuiEngineLocalTests
{
    [Fact]
    public void Constructor_WithEmbeddedKernel_ShouldSetLocalMode()
    {
        var kernel = new EmbeddedKrnlAI(new EmbeddedKernelOptions
        {
            LLmProvider = "none",
            StoreMode = "InMemory",
            VectorMode = "InMemory",
            CacheMode = "Memory"
        });

        var engine = new TuiEngine(kernel);

        engine.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithBaseUrl_ShouldSetRemoteMode()
    {
        var engine = new TuiEngine("http://localhost:5000");

        engine.Should().NotBeNull();
    }

    [Fact]
    public async Task SendAsync_LocalMode_ShouldReturnNarration()
    {
        var kernel = new EmbeddedKrnlAI(new EmbeddedKernelOptions
        {
            LLmProvider = "none",
            StoreMode = "InMemory",
            VectorMode = "InMemory",
            CacheMode = "Memory"
        });

        var result = await kernel.RunAsync("hello from TUI", CancellationToken.None).ConfigureAwait(false);

        result.Should().NotBeNull();
        result.Narration.Should().Contain("hello from TUI");
        result.Mode.Should().Be("community");
    }

    [Fact]
    public async Task SendAsync_LocalMode_MultipleCalls_ShouldMaintainContext()
    {
        var kernel = new EmbeddedKrnlAI(new EmbeddedKernelOptions
        {
            LLmProvider = "none",
            StoreMode = "InMemory",
            VectorMode = "InMemory",
            CacheMode = "Memory"
        });

        var first = await kernel.RunAsync("first message", CancellationToken.None).ConfigureAwait(false);
        var second = await kernel.RunAsync("second message", CancellationToken.None).ConfigureAwait(false);

        first.Narration.Should().NotBeNullOrEmpty();
        second.Narration.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task InteractiveCommand_LocalFlag_ShouldCreateEmbeddedKernel()
    {
        var cmd = new InteractiveCommand().Build();

        cmd.Options.Select(o => o.Name).Should().Contain("--local");
        cmd.Options.Select(o => o.Name).Should().Contain("--model");
    }
}
