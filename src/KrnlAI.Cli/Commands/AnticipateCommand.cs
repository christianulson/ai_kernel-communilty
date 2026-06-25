using System.CommandLine;
using KrnlAI.Cli.Services;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class AnticipateCommand(CliContext ctx, ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("anticipate", "Show active projections");
        var domainOpt = new Option<string>("--domain")
        {
            Description = "Filter by domain"
        };
        cmd.Add(domainOpt);
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var domain = r.GetValue(domainOpt);
            var projections = await ctx.AnticipationService.GetActiveProjectionsAsync(domain, ct).ConfigureAwait(false);
            if (projections.Count == 0)
            {
                renderer.Console.MarkupLine("[yellow]No active projections[/]");
                return 0;
            }

            var rows = projections.Select(p => new
            {
                p.ProjectionId,
                Kind = p.Kind.ToString(),
                p.Domain,
                Desc = (p.Description.Length > 50 ? p.Description[..50] + "..." : p.Description),
                Conf = $"{p.Confidence:F2}",
                Outcome = $"{p.ExpectedOutcome:F2}",
                Risk = $"{p.RiskScore:F2}",
                Horizon = p.Horizon.ToString(),
                Generated = p.GeneratedAt.ToString("HH:mm")
            }).ToList();
            renderer.RenderTable(rows, "ProjectionId", "Kind", "Domain", "Desc", "Conf", "Outcome", "Risk", "Horizon", "Generated");

            var accuracy = await ctx.AnticipationService.GetAnticipationAccuracyAsync(ct).ConfigureAwait(false);
            renderer.Console.MarkupLine($"\n[bold]Anticipation Accuracy:[/] {accuracy:P2}");
            return 0;
        });
        return cmd;
    }
}