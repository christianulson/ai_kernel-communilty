using System.CommandLine;
using AIKernel.Cli.Services;
using Kernel.Core.Services.Safety;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class SafetyCommand(CliContext ctx, ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("safety", "Safety audit and rules");

        var rules = new Command("rules", "List active safety rules");
        rules.SetAction((ParseResult _, CancellationToken _) =>
        {
            var allRules = ctx.RulesEngine.GetAllRules();
            if (allRules.Count == 0)
            {
                renderer.Console.MarkupLine("[yellow]No rules registered[/]");
                return Task.FromResult(0);
            }
            var rows = allRules.Select(r => new
            {
                r.Id,
                r.Title,
                r.Description,
                Severity = r.Severity.ToString(),
                Enabled = r.IsEnabled ? "yes" : "no"
            }).ToList();
            renderer.RenderTable(rows, "Id", "Title", "Description", "Severity", "Enabled");
            return Task.FromResult(0);
        });
        cmd.Add(rules);

        var audit = new Command("audit", "Show recent safety audit records");
        var takeOpt = new Option<int>("--take")
        {
            Description = "Max records", DefaultValueFactory = _ => 20
        };
        audit.Add(takeOpt);
        audit.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var take = r.GetValue(takeOpt);
            var records = await ctx.SafetyCaseStore.ListRecentAsync(take, ct);
            if (records.Count == 0)
            {
                renderer.Console.MarkupLine("[yellow]No safety audit records[/]");
                return 0;
            }
            var rows = records.Select(rec => new
            {
                rec.CaseId,
                Goal = (rec.Goal.Length > 50 ? rec.Goal[..50] + "..." : rec.Goal),
                rec.Status,
                Risk = $"{rec.RiskScore:F2}",
                Probability = $"{rec.ExpectedSuccessProbability:F2}",
                rec.Concerns.Count,
                Created = rec.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            }).ToList();
            renderer.RenderTable(rows, "CaseId", "Goal", "Status", "Risk", "Probability", "Count", "Created");
            return 0;
        });
        cmd.Add(audit);

        return cmd;
    }
}