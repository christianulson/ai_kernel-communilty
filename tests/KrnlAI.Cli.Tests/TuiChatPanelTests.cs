using KrnlAI.Cli.Tui;

namespace KrnlAI.Cli.Tests;

public sealed class TuiChatPanelTests
{
    [Fact]
    public void TuiChatPanel_AddMessage_ShouldIncreaseCount()
    {
        var panel = new TuiChatPanel();
        panel.AddMessage("user", "hello");
        Assert.Equal(1, panel.MessageCount);
    }

    [Fact]
    public void TuiChatPanel_AddMultipleMessages_ShouldTrackCount()
    {
        var panel = new TuiChatPanel();
        panel.AddMessage("user", "hello");
        panel.AddMessage("assistant", "world");
        panel.AddMessage("system", "info");
        Assert.Equal(3, panel.MessageCount);
    }

    [Fact]
    public void TuiChatPanel_MaxMessages_ShouldTrim()
    {
        var panel = new TuiChatPanel();
        for (int i = 0; i < 150; i++)
            panel.AddMessage("user", $"msg {i}");
        Assert.Equal(100, panel.MessageCount);
    }

    [Fact]
    public void TuiChatPanel_Clear_ShouldEmpty()
    {
        var panel = new TuiChatPanel();
        panel.AddMessage("user", "hello");
        panel.Clear();
        Assert.Equal(0, panel.MessageCount);
    }

    [Fact]
    public void TuiChatPanel_Render_ShouldNotThrow()
    {
        var panel = new TuiChatPanel();
        panel.AddMessage("user", "hello");
        panel.AddMessage("assistant", "world");
        var rendered = panel.Render();
        Assert.NotNull(rendered);
    }

    [Fact]
    public void TuiChatPanel_EmptyRender_ShouldNotThrow()
    {
        var panel = new TuiChatPanel();
        var rendered = panel.Render();
        Assert.NotNull(rendered);
    }

    [Fact]
    public void TuiChatPanel_ErrorMessages_ShouldRender()
    {
        var panel = new TuiChatPanel();
        panel.AddMessage("assistant", "something broke", isError: true);
        Assert.Single(panel.Messages);
        Assert.True(panel.Messages[0].IsError);
    }

    [Fact]
    public void TuiChatPanel_Messages_ShouldBeReadonly()
    {
        var panel = new TuiChatPanel();
        panel.AddMessage("user", "test");
        var messages = panel.Messages;
        Assert.Single(messages);
        Assert.Equal("user", messages[0].Role);
        Assert.Equal("test", messages[0].Content);
    }
}
