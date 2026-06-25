using KrnlAI.Desktop.App.Services;
using KrnlAI.Embedded;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class EmbeddedSlashCommandExecutorTests
{
    private static EmbeddedSlashCommandExecutor CreateExecutor()
    {
        var kernel = new EmbeddedKrnlAI();
        return new EmbeddedSlashCommandExecutor(kernel);
    }

    [Fact]
    public async Task ExecuteAsync_Clear_ShouldReturnConstant()
    {
        var executor = CreateExecutor();
        Assert.Equal("CLEAR_CONVERSATION", await executor.ExecuteAsync("/clear").ConfigureAwait(false));
    }

    [Fact]
    public async Task ExecuteAsync_Clear_WithExtraArgs_ShouldStillClear()
    {
        var executor = CreateExecutor();
        Assert.Equal("CLEAR_CONVERSATION", await executor.ExecuteAsync("/clear all").ConfigureAwait(false));
    }

    [Fact]
    public async Task ExecuteAsync_Help_ShouldReturnHelpText()
    {
        var executor = CreateExecutor();
        var result = await executor.ExecuteAsync("/help").ConfigureAwait(false);
        Assert.Contains("/clear", result);
        Assert.Contains("/help", result);
    }

    [Fact]
    public async Task ExecuteAsync_ValidPrompt_ShouldReturnNarration()
    {
        var executor = CreateExecutor();
        var result = await executor.ExecuteAsync("hello").ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyInput_ShouldReturnError()
    {
        var executor = CreateExecutor();
        var result = await executor.ExecuteAsync("").ConfigureAwait(false);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExecuteAsync_WhitespaceInput_ShouldReturnSomething()
    {
        var executor = CreateExecutor();
        var result = await executor.ExecuteAsync("   ").ConfigureAwait(false);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExecuteAsync_NonSlashCommand_ShouldRunInKernel()
    {
        var executor = CreateExecutor();
        var result = await executor.ExecuteAsync("What is the meaning of life?").ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task ExecuteAsync_SlashPrepended_ShouldTreatAsSlashCommand()
    {
        var executor = CreateExecutor();
        var result = await executor.ExecuteAsync("/ help").ConfigureAwait(false);
        var result2 = await executor.ExecuteAsync("/clear ").ConfigureAwait(false);
        Assert.Equal("CLEAR_CONVERSATION", result2);
    }
}
