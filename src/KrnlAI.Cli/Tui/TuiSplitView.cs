using Spectre.Console;

namespace KrnlAI.Cli.Tui;

public sealed class TuiSplitView
{
    private readonly List<Func<Panel>> _panels = [];
    private readonly List<int> _sizes = [];

    public void AddPanel(Func<Panel> renderer, int size = 1)
    {
        _panels.Add(renderer);
        _sizes.Add(size);
    }

    public void Render()
    {
        if (_panels.Count == 0) return;

        if (_panels.Count == 1)
        {
            AnsiConsole.Write(_panels[0]());
            return;
        }

        var grid = new Grid();

        var totalSize = _sizes.Sum();
        for (var i = 0; i < _panels.Count; i++)
        {
            grid.AddColumn(new GridColumn().PadRight(i < _panels.Count - 1 ? 1 : 0));
        }

        var rowItems = _panels.Select(p => p()).ToArray();
        grid.AddRow(rowItems);

        AnsiConsole.Write(grid);
    }

    public void Clear()
    {
        _panels.Clear();
        _sizes.Clear();
    }

    public int PanelCount => _panels.Count;
}
