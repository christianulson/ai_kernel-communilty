using KrnlAI.Desktop.App.Services;
using KrnlAI.Embedded;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class EmbeddedSlashCommandExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_Clear_ShouldReturnConstant()
    {
        await using var kernel = new EmbeddedKrnlAI();
        var executor = new EmbeddedSlashCommandExecutor(kernel);

        var result = await executor.ExecuteAsync("/clear");

        Assert.Equal("CLEAR_CONVERSATION", result);
    }

    [Fact]
    public async Task ExecuteAsync_Help_ShouldReturnHelpText()
    {
        await using var kernel = new EmbeddedKrnlAI();
        var executor = new EmbeddedSlashCommandExecutor(kernel);

        var result = await executor.ExecuteAsync("/help");

        Assert.Contains("/clear", result);
    }

    [Fact]
    public async Task ExecuteAsync_ValidPrompt_ShouldReturnNarration()
    {
        await using var kernel = new EmbeddedKrnlAI();
        var executor = new EmbeddedSlashCommandExecutor(kernel);

        var result = await executor.ExecuteAsync("hello");

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }
}
