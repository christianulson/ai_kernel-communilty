using KrnlAI.Cli.Services;
using KrnlAI.Embedded;

namespace KrnlAI.Cli.Tests;

public sealed class LocalSlashHandlerTests
{
    private static LocalSlashHandler CreateHandler()
    {
        var kernel = new EmbeddedKrnlAI();
        return new LocalSlashHandler(kernel);
    }

    [Fact]
    public async Task ExecuteAsync_Clear_ShouldReturnConstant()
    {
        var handler = CreateHandler();

        var result = await handler.ExecuteAsync("/clear", CancellationToken.None);

        Assert.Equal("CLEAR_CONVERSATION", result);
    }

    [Fact]
    public async Task ExecuteAsync_Help_ShouldReturnHelpText()
    {
        var handler = CreateHandler();

        var result = await handler.ExecuteAsync("/help", CancellationToken.None);

        Assert.Contains("/clear", result);
    }

    [Fact]
    public async Task ExecuteAsync_ValidPrompt_ShouldReturnNarration()
    {
        var handler = CreateHandler();

        var result = await handler.ExecuteAsync("hello", CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }
}
