using System.CommandLine;
using System.Text.Json;
using KrnlAI.Cli.Services;
using KrnlAI.Core.Abstractions;
using KrnlAI.Core.Abstractions.Safety;
using KrnlAI.Core.Model;
using KrnlAI.Core.Services.Safety;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

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
        var schedule = new Command("schedule", "Run or schedule safety audits");
        var intervalOpt = new Option<int>("--interval") { Description = "Interval in hours between audits (0 = no scheduling)" };
        var outputOpt = new Option<string>("--output") { Description = "Output file path for the report" };
        var runOnceOpt = new Option<bool>("--run-once") { Description = "Run once and exit (overrides --interval)" };
        schedule.Add(intervalOpt);
        schedule.Add(outputOpt);
        schedule.Add(runOnceOpt);
        schedule.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var interval = r.GetValue(intervalOpt);
            var output = r.GetValue(outputOpt);
            var runOnce = r.GetValue(runOnceOpt);
            var auditStore = serviceProvider.GetService<ISafetyAuditStore>();

            renderer.Console.MarkupLine("[cyan]Running safety audit...[/]");
            var runner = new SafetyBenchRunner(ctx.RulesEngine, null, NullLogger<SafetyBenchRunner>.Instance);

            var report = await runner.RunBenchmarkAsync(AttackVectors.All, "cli-schedule", ct);

            if (auditStore is not null)
                await auditStore.SaveReportAsync(report, ct);

            if (output is not null)
            {
                var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(output, json, ct);
                renderer.Console.MarkupLine($"[green]Report saved to {output}[/]");
            }

            renderer.Console.MarkupLine(report.Passed == report.TotalScenarios
                ? "[green]Audit PASSED[/]"
                : $"[yellow]Audit: {report.Passed}/{report.TotalScenarios} passed[/]");

            if (!runOnce && interval > 0)
            {
                var scheduler = serviceProvider.GetService<ISchedulerService>();
                if (scheduler is not null)
                {
                    var nextRun = DateTimeOffset.UtcNow.AddHours(interval);
                    var action = new ScheduledAction(
                        $"safety-audit-{Guid.NewGuid():N}",
                        $"Recurring safety audit every {interval}h",
                        nextRun,
                        "{}",
                        "safety",
                        new ActionRecurrence(RecurrenceType.Hours, interval));
                    await scheduler.ScheduleAsync(action, ct);
                    renderer.Console.MarkupLine($"[cyan]Next audit scheduled in {interval}h at {nextRun:yyyy-MM-dd HH:mm} UTC[/]");
                }
            }

            return report.Failed == 0 ? 0 : 1;
        });
        cmd.Add(schedule);

        // ── compliance (P4) ────────────────────────────────
        var compliance = new Command("compliance", "Show compliance coverage for fundamental rules");
        var formatOpt = new Option<string>("--format") { Description = "Output format: table, json" };
        var rulesOpt = new Option<string>("--rules") { Description = "Rule filter (comma-separated or range, e.g. R01,R02 or R01-R10)" };
        compliance.Add(formatOpt);
        compliance.Add(rulesOpt);
        compliance.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var format = r.GetValue(formatOpt) ?? "table";
            var rulesFilter = r.GetValue(rulesOpt);
            var analyzer = new ComplianceAnalyzer();
            var scenarioStore = serviceProvider.GetService<ISafetyScenarioStore>();
            var auditStore = serviceProvider.GetService<ISafetyAuditStore>();

            var coverage = await analyzer.AnalyzeCoverageAsync(ctx.RulesEngine, scenarioStore!, auditStore!);

            var filtered = ApplyRulesFilter(coverage.Rules, rulesFilter);

            var rows = filtered.Select(c => new
            {
                c.RuleId, c.Description,
                Coverage = $"{c.EffectivenessScore:P1}",
                c.TimesTriggered, c.TimesBypassed,
                Active = c.IsActive ? "yes" : "no"
            }).ToList();

            if (format == "json")
            {
                var filteredCoverage = coverage with { Rules = filtered };
                var json = JsonSerializer.Serialize(filteredCoverage, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine(json);
            }
            else
            {
                renderer.RenderTable(rows, "RuleId", "Description", "Coverage", "TimesTriggered", "TimesBypassed", "Active");
                renderer.Console.MarkupLine($"\n[bold]Overall compliance:[/] {coverage.OverallCoverage:P2}");
                if (rulesFilter is not null)
                    renderer.Console.MarkupLine($"[dim]Showing {filtered.Count} of {coverage.Rules.Count} rules[/]");
            }
            return 0;
        });
        cmd.Add(compliance);

        return cmd;
    }

    private static IReadOnlyList<RuleCoverage> ApplyRulesFilter(IReadOnlyList<RuleCoverage> rules, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return rules;

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in filter.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Contains('-'))
            {
                var parts = token.Split('-', 2);
                if (parts.Length == 2 && TryParseRuleId(parts[0], out var start) && TryParseRuleId(parts[1], out var end))
                {
                    for (var i = start; i <= end; i++)
                        ids.Add($"R{i:D2}");
                }
            }
            else
            {
                ids.Add(token.ToUpperInvariant());
            }
        }

        return rules.Where(r => ids.Contains(r.RuleId)).ToList();
    }

    private static bool TryParseRuleId(string s, out int num)
    {
        num = 0;
        var cleaned = s.TrimStart('R', 'r');
        return int.TryParse(cleaned, out num);
    }
}
