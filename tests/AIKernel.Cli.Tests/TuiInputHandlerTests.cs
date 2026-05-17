using AIKernel.Cli.Tui;

namespace AIKernel.Cli.Tests;

public sealed class TuiInputHandlerTests
{
    private static TuiInputHandler CreateHandler()
    {
        return new TuiInputHandler(new Dictionary<string, string>
        {
            ["/help"] = "Show help",
            ["/explain"] = "Explain code",
            ["/exit"] = "Exit",
            ["/sessions"] = "List sessions",
        });
    }

    [Fact]
    public void TuiInputHandler_ParseSlashCommand_ShouldReturnCommand()
    {
        var handler = CreateHandler();
        var (cmd, args) = handler.Parse("/explain this code");
        Assert.Equal("/explain", cmd);
        Assert.Equal("this code", args);
    }

    [Fact]
    public void TuiInputHandler_ParseNoSlash_ShouldReturnEmpty()
    {
        var handler = CreateHandler();
        var (cmd, args) = handler.Parse("hello world");
        Assert.Equal("", cmd);
        Assert.Equal("hello world", args);
    }

    [Fact]
    public void TuiInputHandler_ParseSlashOnly_ShouldReturnCommandOnly()
    {
        var handler = CreateHandler();
        var (cmd, args) = handler.Parse("/help");
        Assert.Equal("/help", cmd);
        Assert.Equal("", args);
    }

    [Fact]
    public void TuiInputHandler_Autocomplete_ShouldMatchSingleCommand()
    {
        var handler = CreateHandler();
        var result = handler.Autocomplete("/sess");
        Assert.NotNull(result);
        Assert.Equal("/sessions", result);
    }

    [Fact]
    public void TuiInputHandler_Autocomplete_NoMatch_ShouldReturnNull()
    {
        var handler = CreateHandler();
        var result = handler.Autocomplete("/xyz");
        Assert.Null(result);
    }

    [Fact]
    public void TuiInputHandler_Autocomplete_NoSlash_ShouldReturnNull()
    {
        var handler = CreateHandler();
        var result = handler.Autocomplete("hello");
        Assert.Null(result);
    }

    [Fact]
    public void TuiInputHandler_IsKnownCommand_ShouldFind()
    {
        var handler = CreateHandler();
        Assert.True(handler.IsKnownCommand("/help"));
        Assert.False(handler.IsKnownCommand("/unknown"));
    }

    [Fact]
    public void TuiInputHandler_AddCommand_ShouldExtend()
    {
        var handler = CreateHandler();
        handler.AddCommand("/newcmd", "New command");
        Assert.True(handler.IsKnownCommand("/newcmd"));
    }

    [Fact]
    public void TuiInputHandler_GetCommandDescriptions_ShouldReturnAll()
    {
        var handler = CreateHandler();
        var cmds = handler.GetCommandDescriptions();
        Assert.Equal(4, cmds.Count);
        Assert.Contains("/help", cmds.Keys);
        Assert.Contains("/exit", cmds.Keys);
    }
}
