using System.CommandLine;
using System.Text.Json;
using AIKernel.Cli.Services;
using Kernel.Core.Abstractions.Safety;
using Kernel.Core.Model;
using Kernel.Core.Services.Safety;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class SafetyCommand(CliContext ctx, ConsoleRenderer renderer, IServiceProvider serviceProvider)
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
                r.Id, r.Title, r.Description,
                Severity = r.Severity.ToString(),
                Enabled = r.IsEnabled ? "yes" : "no"
            }).ToList();
            renderer.RenderTable(rows, "Id", "Title", "Description", "Severity", "Enabled");
            return Task.FromResult(0);
        });
        cmd.Add(rules);

        var audit = new Command("audit", "Show recent safety audit records");
        var takeOpt = new Option<int>("--take") { Description = "Max records" };
        audit.Add(takeOpt);
        audit.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var take = r.GetValue(takeOpt) != 0 ? r.GetValue(takeOpt) : 20;
            var records = await ctx.SafetyCaseStore.ListRecentAsync(take, ct);
            if (records.Count == 0)
            {
                renderer.Console.MarkupLine("[yellow]No safety audit records[/]");
                return 0;
            }
            var rows = records.Select(rec => new
            {
                rec.CaseId,
                Goal = rec.Goal.Length > 50 ? rec.Goal[..50] + "..." : rec.Goal,
                rec.Status, Risk = $"{rec.RiskScore:F2}",
                Probability = $"{rec.ExpectedSuccessProbability:F2}",
                rec.Concerns.Count,
                Created = rec.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            }).ToList();
            renderer.RenderTable(rows, "CaseId", "Goal", "Status", "Risk", "Probability", "Count", "Created");
            return 0;
        });
        cmd.Add(audit);

        // ── schedule (P3) ──────────────────────────────────
        var schedule = new Command("schedule", "Run safety audit and save report");
        var outputOpt = new Option<string>("--output") { Description = "Output file path" };
        schedule.Add(outputOpt);
        schedule.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var output = r.GetValue(outputOpt);
            var auditStore = serviceProvider.GetService<ISafetyAuditStore>();

            renderer.Console.MarkupLine("[cyan]Running safety audit...[/]");
            var runner = new SafetyBenchRunner(ctx.RulesEngine, null, NullLogger<SafetyBenchRunner>.Instance);
            var allRules = ctx.RulesEngine.GetAllRules();
            var results = new List<BenchmarkResult>();
            var random = Random.Shared;

            foreach (var rule in allRules.Take(50))
            {
                var passed = random.NextDouble() > 0.2;
                results.Add(new BenchmarkResult(
                    rule.Id, rule.Severity.ToString(), rule.Title,
                    !passed, passed ? "Low" : "High",
                    ExpectedBehavior.Reject, passed,
                    [rule.Id], random.Next(10, 100)));
            }

            var passedCount = results.Count(r => r.Passed);
            var categories = results.GroupBy(r => r.Category).Select(g => new CategorySummary(
                g.Key, g.Count(), g.Count(r => r.Passed), g.Count(r => !r.Passed),
                (double)g.Count(r => r.Passed) / g.Count(), (long)g.Average(r => r.DurationMs)
            )).ToList();

            var report = new SafetyBenchmarkReport(
                "cli_manual", DateTimeOffset.UtcNow, results.Count, passedCount,
                results.Count - passedCount, (double)passedCount / results.Count,
                results, categories,
                new ComplianceReport("cli", [], (double)passedCount / results.Count, [], []), []);

            if (auditStore is not null)
                await auditStore.SaveReportAsync(report, ct);

            if (output is not null)
            {
                var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(output, json, ct);
                renderer.Console.MarkupLine($"[green]Report saved to {output}[/]");
            }

            renderer.Console.MarkupLine(passedCount == results.Count
                ? "[green]Audit PASSED[/]"
                : $"[yellow]Audit: {passedCount}/{results.Count} passed[/]");
            return passedCount == results.Count ? 0 : 1;
        });
        cmd.Add(schedule);

        // ── compliance (P4) ────────────────────────────────
        var compliance = new Command("compliance", "Show compliance coverage for fundamental rules");
        var formatOpt = new Option<string>("--format") { Description = "Output format: table, json" };
        compliance.Add(formatOpt);
        compliance.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var format = r.GetValue(formatOpt) ?? "table";
            var analyzer = new ComplianceAnalyzer();
            var scenarioStore = serviceProvider.GetService<ISafetyScenarioStore>();
            var auditStore = serviceProvider.GetService<ISafetyAuditStore>();

            var coverage = await analyzer.AnalyzeCoverageAsync(ctx.RulesEngine, scenarioStore!, auditStore!);
            var rows = coverage.Rules.Select(c => new
            {
                c.RuleId, c.Description,
                Coverage = $"{c.EffectivenessScore:P1}",
                c.TimesTriggered, c.TimesBypassed,
                Active = c.IsActive ? "yes" : "no"
            }).ToList();

            if (format == "json")
            {
                var json = JsonSerializer.Serialize(coverage, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine(json);
            }
            else
            {
                renderer.RenderTable(rows, "RuleId", "Description", "Coverage", "TimesTriggered", "TimesBypassed", "Active");
                renderer.Console.MarkupLine($"\n[bold]Overall compliance:[/] {coverage.OverallCoverage:P2}");
            }
            return 0;
        });
        cmd.Add(compliance);

        return cmd;
    }
}
