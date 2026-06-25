using KrnlAI.Cli.Services;
using KrnlAI.Embedded;

namespace KrnlAI.Cli.Tests;

public sealed class LocalSlashHandlerTests
{
    private static LocalSlashHandler Create()
    {
        return new LocalSlashHandler(new EmbeddedKrnlAI());
    }

    [Fact]
    public async Task ExecuteAsync_Clear_ShouldReturnConstant()
    {
        var handler = Create();
        Assert.Equal("CLEAR_CONVERSATION", await handler.ExecuteAsync("/clear").ConfigureAwait(false));
    }

    [Fact]
    public async Task ExecuteAsync_Clear_WithArgs_ShouldStillClear()
    {
        var handler = Create();
        Assert.Equal("CLEAR_CONVERSATION", await handler.ExecuteAsync("/clear all").ConfigureAwait(false));
    }

    [Fact]
    public async Task ExecuteAsync_Help_ShouldReturnHelpText()
    {
        var handler = Create();
        var result = await handler.ExecuteAsync("/help").ConfigureAwait(false);
        Assert.Contains("/clear", result);
    }

    [Fact]
    public async Task ExecuteAsync_Help_WithArgs_ShouldStillShowHelp()
    {
        var handler = Create();
        var result = await handler.ExecuteAsync("/help commands").ConfigureAwait(false);
        Assert.Contains("/clear", result);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownSlash_ShouldRunInKernel()
    {
        var handler = Create();
        var result = await handler.ExecuteAsync("/nonexistent_cmd_xyz").ConfigureAwait(false);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExecuteAsync_ValidPrompt_ShouldReturnNarration()
    {
        var handler = Create();
        var result = await handler.ExecuteAsync("hello").ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyInput_ShouldNotThrow()
    {
        var handler = Create();
        var result = await handler.ExecuteAsync("").ConfigureAwait(false);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExecuteAsync_Whitespace_ShouldNotThrow()
    {
        var handler = Create();
        var result = await handler.ExecuteAsync("   ").ConfigureAwait(false);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExecuteAsync_LongPrompt_ShouldReturn()
    {
        var handler = Create();
        var longInput = new string('x', 1000);
        var result = await handler.ExecuteAsync(longInput).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleCalls_ShouldWork()
    {
        var handler = Create();
        for (var i = 0; i < 5; i++)
        {
            var result = await handler.ExecuteAsync($"call number {i}").ConfigureAwait(false);
            Assert.NotNull(result);
        }
    }
}
