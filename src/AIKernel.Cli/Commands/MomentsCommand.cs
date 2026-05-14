using System.CommandLine;
using AIKernel.Cli.Services;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class MomentsCommand(CliContext ctx, ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("moments", "Manage moments");
        var take = new Option<int>("--take")
        {
            Description = "Number of moments",
            DefaultValueFactory = _ => 10
        };

        var recent = new Command("recent", "Show recent moments")
        {
            take
        };
        recent.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var takeVal = r.GetValue(take);
            var moments = await ctx.MomentStore.ListRecentAsync(takeVal, ct);
            var rows = new List<object>();
            foreach (var m in moments)
            {
                var c = await ctx.MomentClassifierStore.GetAsync(m.MomentId, ct);
                rows.Add(new
                {
                    m.MomentId,
                    Sequence = m.Sequence.ToString(),
                    Domain = m.FocusDomain?.ToString() ?? "",
                    Category = c?.Category.ToString() ?? "",
                    Importance = m.CognitiveLoad.ToString("F2")
                });
            }
            renderer.RenderTable(rows, "MomentId", "Sequence", "Domain", "Category", "Importance");
            return 0;
        });
        cmd.Add(recent);

        var idArg = new Argument<string>("id")
        {
            Description = "Moment ID"
        };
        var detail = new Command("get", "Show moment details")
        {
            idArg
        };
        detail.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var id = r.GetValue(idArg)!;
            var moment = await ctx.MomentStore.GetAsync(id, ct);
            if (moment is null)
            {
                renderer.Console.MarkupLine($"[red]Moment '{id}' not found[/]");
                return 1;
            }
            var classification = await ctx.MomentClassifierStore.GetAsync(id, ct);

            renderer.Console.MarkupLine($"[bold]MomentId:[/] {moment.MomentId}");
            renderer.Console.MarkupLine($"[bold]Sequence:[/] {moment.Sequence}");
            renderer.Console.MarkupLine($"[bold]Started:[/] {moment.StartedAt:yyyy-MM-dd HH:mm:ss} UTC");
            renderer.Console.MarkupLine($"[bold]Ended:[/] {moment.EndedAt:yyyy-MM-dd HH:mm:ss} UTC ({moment.EndedAt - moment.StartedAt:g})");
            renderer.Console.MarkupLine($"[bold]Domain:[/] {moment.FocusDomain?.ToString() ?? "N/A"}");
            renderer.Console.MarkupLine($"[bold]Category:[/] {classification?.Category.ToString() ?? "N/A"} (confidence: {classification?.Confidence:F2})");
            renderer.Console.MarkupLine($"[bold]Cognitive Load:[/] {moment.CognitiveLoad:F2}");
            renderer.Console.MarkupLine($"[bold]Arousal:[/] {moment.Arousal:F2}");
            renderer.Console.MarkupLine($"[bold]Valence:[/] {moment.Valence:F2}");
            renderer.Console.MarkupLine($"[bold]Stimuli:[/] {moment.Stimuli.Count} signals");
            renderer.Console.MarkupLine($"[bold]Bindings:[/] {moment.Bindings.Count} cross-modal");
            return 0;
        });
        cmd.Add(detail);

        return cmd;
    }
}
