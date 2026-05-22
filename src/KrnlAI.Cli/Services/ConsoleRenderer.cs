using KrnlAI.Core.Abstractions;
using KrnlAI.Core.Models;
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

    public void RenderPlanArtifact(PlanArtifact artifact)
    {
        Console.MarkupLine($"\n[bold cyan]Plan Artifact[/]");
        Console.MarkupLine($"  [bold]ID:[/]      {artifact.Id}");
        Console.MarkupLine($"  [bold]Goal:[/]    {artifact.Goal.EscapeMarkup()}");
        Console.MarkupLine($"  [bold]Created:[/] {artifact.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        if (!string.IsNullOrEmpty(artifact.Content))
        {
            Console.MarkupLine($"\n[bold]Content:[/]");
            Console.WriteLine(artifact.Content);
        }
    }

    public void RenderPlanList(IReadOnlyList<PlanArtifact> plans)
    {
        if (plans.Count == 0)
        {
            Console.MarkupLine("[yellow]No plan artifacts[/]");
            return;
        }
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("ID");
        table.AddColumn("Goal");
        table.AddColumn("Created");
        table.AddColumn("Has Content");
        foreach (var plan in plans)
        {
            table.AddRow(
                plan.Id[..8] + "...",
                plan.Goal.Length > 50 ? plan.Goal[..50] + "..." : plan.Goal.EscapeMarkup(),
                plan.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                string.IsNullOrEmpty(plan.Content) ? "[red]No[/]" : "[green]Yes[/]");
        }
        Console.Write(table);
    }

    public void RenderError(string message)
    {
        Console.MarkupLine($"[red]Error:[/] {message.EscapeMarkup()}");
    }

    public void RenderSuccess(string message)
    {
        Console.MarkupLine($"[green]{message.EscapeMarkup()}[/]");
    }

    public void RenderCheckpointList(IReadOnlyList<CheckpointInfo> checkpoints)
    {
        if (checkpoints.Count == 0)
        {
            Console.MarkupLine("[yellow]No checkpoints[/]");
            return;
        }
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("ID");
        table.AddColumn("Label");
        table.AddColumn("Created");
        table.AddColumn("Files");
        foreach (var cp in checkpoints)
        {
            table.AddRow(
                cp.Id[..8] + "...",
                cp.Label.EscapeMarkup(),
                cp.CreatedAt.ToString("HH:mm:ss"),
                cp.FileCount.ToString());
        }
        Console.Write(table);
    }

    public void RenderDiff(string diff)
    {
        if (string.IsNullOrEmpty(diff))
        {
            Console.MarkupLine("[yellow]No diff[/]");
            return;
        }
        Console.MarkupLine("[bold]Diff:[/]");
        Console.WriteLine(diff);
    }
}
