using System.CommandLine;
using AIKernel.Cli.Services;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class ModelCommand(CliContext ctx, ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("model", "Manage AI models");

        cmd.Add(BuildList());
        cmd.Add(BuildGet());
        cmd.Add(BuildVersions());

        return cmd;
    }

    private Command BuildList()
    {
        var cmd = new Command("list", "List registered models");
        cmd.SetAction((ParseResult _, CancellationToken _) =>
        {
            var models = ctx.ModelRegistry.ListModels();
            if (models.Count == 0)
            {
                renderer.Console.MarkupLine("[yellow]No models registered[/]");
                return Task.FromResult(0);
            }
            var rows = models.Select(m => new
            {
                ModelId = m,
                Production = ctx.ModelRegistry.GetProductionVersion(m) ?? "none"
            }).ToList();
            renderer.RenderTable(rows, "ModelId", "Production");
            return Task.FromResult(0);
        });
        return cmd;
    }

    private Command BuildGet()
    {
        var idArg = new Argument<string>("id") { Description = "Model ID" };
        var cmd = new Command("get", "Show model details") { idArg };
        cmd.SetAction((ParseResult r, CancellationToken _) =>
        {
            var id = r.GetValue(idArg)!;
            var model = ctx.ModelRegistry.GetModel(id);
            if (model is null)
            {
                renderer.Console.MarkupLine($"[red]Model '{id}' not found[/]");
                return Task.FromResult(1);
            }
            renderer.Console.MarkupLine($"[bold]ModelId:[/] {model.ModelId}");
            renderer.Console.MarkupLine($"[bold]Version:[/] {model.Version}");
            renderer.Console.MarkupLine($"[bold]Description:[/] {model.Description}");
            renderer.Console.MarkupLine($"[bold]Registered:[/] {model.RegisteredAt:yyyy-MM-dd HH:mm:ss}");
            return Task.FromResult(0);
        });
        return cmd;
    }

    private Command BuildVersions()
    {
        var idArg = new Argument<string>("id") { Description = "Model ID" };
        var cmd = new Command("versions", "List model versions") { idArg };
        cmd.SetAction((ParseResult r, CancellationToken _) =>
        {
            var id = r.GetValue(idArg)!;
            var versions = ctx.ModelRegistry.ListVersions(id);
            if (versions.Count == 0)
            {
                renderer.Console.MarkupLine($"[yellow]No versions for model '{id}'[/]");
                return Task.FromResult(0);
            }
            var rows = versions.Select(v => new
            {
                v.Version,
                v.Description,
                Registered = v.RegisteredAt.ToString("yyyy-MM-dd HH:mm")
            }).ToList();
            renderer.RenderTable(rows, "Version", "Description", "Registered");
            return Task.FromResult(0);
        });
        return cmd;
    }
}
