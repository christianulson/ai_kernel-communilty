using KrnlAI.Cli.Tui;

namespace KrnlAI.Cli.Tests;

public sealed class TuiStatusPanelTests
{
    [Fact]
    public void TuiStatusPanel_Defaults_ShouldBeSet()
    {
        var panel = new TuiStatusPanel();
        Assert.Equal("Desconectado", panel.Status);
        Assert.Equal("0.0", panel.RiskLevel);
        Assert.Equal("Chat", panel.Mode);
    }

    [Fact]
    public void TuiStatusPanel_UpdateProperties_ShouldReflect()
    {
        var panel = new TuiStatusPanel
        {
            Status = "Conectado",
            RiskLevel = "0.5",
            Mode = "SafeAgent",
            MemoryCount = "42",
            MessageCount = 7
        };
        Assert.Equal("Conectado", panel.Status);
        Assert.Equal("0.5", panel.RiskLevel);
        Assert.Equal("SafeAgent", panel.Mode);
        Assert.Equal("42", panel.MemoryCount);
        Assert.Equal(7, panel.MessageCount);
    }

    [Fact]
    public void TuiStatusPanel_Render_ShouldNotThrow()
    {
        var panel = new TuiStatusPanel();
        var rendered = panel.Render();
        Assert.NotNull(rendered);
    }

    [Fact]
    public void TuiStatusPanel_RenderWithHighRisk_ShouldNotThrow()
    {
        var panel = new TuiStatusPanel
        {
            Status = "Conectado",
            RiskLevel = "0.9",
            Mode = "FullAgent"
        };
        var rendered = panel.Render();
        Assert.NotNull(rendered);
    }

    [Fact]
    public void TuiStatusPanel_Mood_ShouldReflect()
    {
        var panel = new TuiStatusPanel { Mood = "Animado" };
        Assert.Equal("Animado", panel.Mood);
    }

    [Fact]
    public void TuiStatusPanel_LastAction_ShouldReflect()
    {
        var panel = new TuiStatusPanel { LastAction = "explain" };
        Assert.Equal("explain", panel.LastAction);
    }
}
