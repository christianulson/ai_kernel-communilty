using System.CommandLine;
using AIKernel.Cli.Services;
using Kernel.Core.Services.ExperimentTracking;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class ExperimentCommand(CliContext ctx, ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("experiment", "Manage experiments");

        cmd.Add(BuildList());
        cmd.Add(BuildCreate());
        cmd.Add(BuildGet());
        cmd.Add(BuildMetrics());

        return cmd;
    }

    private Command BuildList()
    {
        var cmd = new Command("list", "List experiments");
        cmd.SetAction((ParseResult _, CancellationToken _) =>
        {
            var experiments = ctx.ExperimentTracker.ListExperiments();
            if (experiments.Count == 0)
            {
                renderer.Console.MarkupLine("[yellow]No experiments[/]");
                return Task.FromResult(0);
            }
            var rows = experiments.Select(e => new
            {
                e.RunId,
                e.ExperimentName,
                e.Variant,
                e.Status,
                Started = e.StartAt.ToString("yyyy-MM-dd HH:mm")
            }).ToList();
            renderer.RenderTable(rows, "RunId", "ExperimentName", "Variant", "Status", "Started");
            return Task.FromResult(0);
        });
        return cmd;
    }

    private Command BuildCreate()
    {
        var nameArg = new Argument<string>("name") { Description = "Experiment name" };
        var variantOpt = new Option<string>("--variant")
        {
            Description = "Experiment variant",
            DefaultValueFactory = _ => "default"
        };
        var descriptionOpt = new Option<string>("--description")
        {
            Description = "Experiment description"
        };

        var cmd = new Command("create", "Create a new experiment")
        {
            nameArg, variantOpt, descriptionOpt
        };

        cmd.SetAction((ParseResult r, CancellationToken _) =>
        {
            var name = r.GetValue(nameArg)!;
            var variant = r.GetValue(variantOpt)!;
            var runId = $"exp-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}";

            var experiment = new ExperimentRun(
                RunId: runId,
                ExperimentName: name,
                Variant: variant,
                StartAt: DateTimeOffset.UtcNow);

            ctx.ExperimentTracker.StartExperiment(experiment);
            renderer.Console.MarkupLine($"[green]Experiment created:[/] {runId}");
            renderer.Console.MarkupLine($"  Name: {name}");
            renderer.Console.MarkupLine($"  Variant: {variant}");
            return Task.FromResult(0);
        });

        return cmd;
    }

    private Command BuildGet()
    {
        var idArg = new Argument<string>("run-id") { Description = "Experiment run ID" };
        var cmd = new Command("get", "Show experiment details") { idArg };
        cmd.SetAction((ParseResult r, CancellationToken _) =>
        {
            var id = r.GetValue(idArg)!;
            var exp = ctx.ExperimentTracker.GetExperiment(id);
            if (exp is null)
            {
                renderer.Console.MarkupLine($"[red]Experiment '{id}' not found[/]");
                return Task.FromResult(1);
            }
            renderer.Console.MarkupLine($"[bold]RunId:[/] {exp.RunId}");
            renderer.Console.MarkupLine($"[bold]Name:[/] {exp.ExperimentName}");
            renderer.Console.MarkupLine($"[bold]Variant:[/] {exp.Variant}");
            renderer.Console.MarkupLine($"[bold]Started:[/] {exp.StartAt:yyyy-MM-dd HH:mm:ss}");
            renderer.Console.MarkupLine($"[bold]Status:[/] {exp.Status}");
            return Task.FromResult(0);
        });
        return cmd;
    }

    private Command BuildMetrics()
    {
        var idArg = new Argument<string>("run-id") { Description = "Experiment run ID" };
        var cmd = new Command("metrics", "Show experiment metrics") { idArg };
        cmd.SetAction((ParseResult r, CancellationToken _) =>
        {
            var id = r.GetValue(idArg)!;
            var exp = ctx.ExperimentTracker.GetExperiment(id);
            if (exp is null)
            {
                renderer.Console.MarkupLine($"[red]Experiment '{id}' not found[/]");
                return Task.FromResult(1);
            }

            var metrics = ctx.ExperimentTracker.GetMetrics(id);
            if (metrics.Count == 0)
            {
                renderer.Console.MarkupLine($"[yellow]No metrics for experiment '{id}'[/]");
                return Task.FromResult(0);
            }

            var rows = metrics.Select(m => new
            {
                Metric = m.Key,
                Value = $"{m.Value:F4}"
            }).ToList();
            renderer.RenderTable(rows, "Metric", "Value");
            return Task.FromResult(0);
        });
        return cmd;
    }
}
