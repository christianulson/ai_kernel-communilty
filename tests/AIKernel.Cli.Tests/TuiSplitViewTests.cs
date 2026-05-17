using AIKernel.Cli.Tui;
using Spectre.Console;

namespace AIKernel.Cli.Tests;

public sealed class TuiSplitViewTests
{
    [Fact]
    public void TuiSplitView_Empty_ShouldHaveZeroPanels()
    {
        var split = new TuiSplitView();
        Assert.Equal(0, split.PanelCount);
    }

    [Fact]
    public void TuiSplitView_AddPanel_ShouldIncreaseCount()
    {
        var split = new TuiSplitView();
        split.AddPanel(() => new Panel("test").Header("H"));
        Assert.Equal(1, split.PanelCount);
    }

    [Fact]
    public void TuiSplitView_MultiplePanels_ShouldTrackCount()
    {
        var split = new TuiSplitView();
        split.AddPanel(() => new Panel("a").Header("A"));
        split.AddPanel(() => new Panel("b").Header("B"));
        Assert.Equal(2, split.PanelCount);
    }

    [Fact]
    public void TuiSplitView_Clear_ShouldReset()
    {
        var split = new TuiSplitView();
        split.AddPanel(() => new Panel("a").Header("A"));
        split.Clear();
        Assert.Equal(0, split.PanelCount);
    }
}
