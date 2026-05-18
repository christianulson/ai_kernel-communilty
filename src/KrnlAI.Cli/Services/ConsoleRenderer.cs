using KrnlAI.Core.Services.Memory;
using Spectre.Console;

namespace KrnlAI.Cli.Services;

public sealed class ConsoleRenderer
{
    public IAnsiConsole Console { get; }

    public ConsoleRenderer(IAnsiConsole console)
    {
        Console = console;
    }

    public void RenderTable<T>(IReadOnlyList<T> items, params string[] columns)
    {
        if (items.Count == 0)
        {
            Console.MarkupLine("[yellow]No data[/]");
            return;
        }

        var table = new Table().Border(TableBorder.Rounded);
        foreach (var col in columns)
            table.AddColumn(new TableColumn(col));

        foreach (var item in items)
        {
            var props = item!.GetType().GetProperties();
            var values = columns.Select(c =>
            {
                var prop = props.FirstOrDefault(p =>
                    string.Equals(p.Name, c, StringComparison.OrdinalIgnoreCase));
                return prop?.GetValue(item)?.ToString() ?? "";
            }).ToArray();
            table.AddRow(values);
        }

        Console.Write(table);
    }

    public void RenderDetail(object item)
    {
        var props = item.GetType().GetProperties();
        foreach (var prop in props)
        {
            var value = prop.GetValue(item)?.ToString() ?? "";
            Console.MarkupLine($"[bold]{prop.Name}:[/] {value.EscapeMarkup()}");
        }
    }

    public void RenderStatusFallback()
    {
        Console.MarkupLine("[yellow]Homeostasis data not available. Starting up...[/]");
    }

    public void RenderStatus(
        CognitiveState state,
        string status,
        string mode,
        int activeGoals,
        int pendingIntentions,
        int activeProjections,
        string lastCycle,
        string cycleDuration)
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow("Status:", status);
        grid.AddRow("Cognitive Load:", $"{state.Fatigue + state.StarvationForNovelty + state.SleepPressure:F2}");
        grid.AddRow("Health Score:", $"{state.HealthScore:F2}");
        grid.AddRow("Mode:", mode);
        grid.AddRow("Active Goals:", activeGoals.ToString());
        grid.AddRow("Pending Intentions:", pendingIntentions.ToString());
        grid.AddRow("Active Projections:", activeProjections.ToString());
        grid.AddRow("Last Cycle:", $"{lastCycle} ({cycleDuration})");

        Console.Write(new Panel(grid).Header("Status").RoundedBorder());
    }

    public void RenderStatusVerbose(CognitiveState state, string status, string mode)
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow("Status:", status);
        grid.AddRow("Mode:", mode);
        grid.AddRow("Fatigue:", $"{state.Fatigue:F2}");
        grid.AddRow("Starvation:", $"{state.StarvationForNovelty:F2}");
        grid.AddRow("Sleep Pressure:", $"{state.SleepPressure:F2}");
        grid.AddRow("Health Score:", $"{state.HealthScore:F2}");
        grid.AddRow("Flags:", state.Flags.ToString());

        Console.Write(new Panel(grid).Header("Status (verbose)").RoundedBorder());
    }

    public void RenderHealth(IReadOnlyList<object> modules, string overall)
    {
        foreach (var mod in modules)
        {
            var props = mod.GetType().GetProperties();
            var name = props.First(p => p.Name == "Name").GetValue(mod)?.ToString() ?? "";
            var status = props.First(p => p.Name == "Status").GetValue(mod)?.ToString() ?? "";
            var latency = props.First(p => p.Name == "Latency").GetValue(mod)?.ToString() ?? "";

            var icon = status switch
            {
                "ok" => "[green]PASS[/]",
                "degraded" => "[yellow]WARN[/]",
                _ => "[red]FAIL[/]"
            };
            Console.MarkupLine($"{icon} [bold]{name}[/] {status} ({latency})");
        }
        Console.MarkupLine($"\nOverall: [bold]{overall}[/]");
    }
}
