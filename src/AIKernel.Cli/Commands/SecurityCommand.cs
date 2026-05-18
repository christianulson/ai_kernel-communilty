using System.CommandLine;
using System.Text.Json;
using Kernel.Core.Model;
using Kernel.Core.Services.Safety;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class SecurityCommand(SafetyBenchRunner benchRunner, IAnsiConsole console)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public Command Build()
    {
        var cmd = new Command("security", "Safety evaluation and benchmarking");

        cmd.Add(BuildAuditCommand());
        cmd.Add(BuildBenchmarkCommand());
        cmd.Add(BuildReportCommand());

        return cmd;
    }

    private Command BuildAuditCommand()
    {
        var configOpt = new Option<string>("--config")
        {
            Description = "Configuration name",
            DefaultValueFactory = _ => "default"
        };
        var formatOpt = new Option<string>("--format")
        {
            Description = "Output format (table|json)",
            DefaultValueFactory = _ => "table"
        };
        var outputOpt = new Option<FileInfo>("--output")
        {
            Description = "Output file path"
        };

        var cmd = new Command("audit", "Run full safety audit against all attack vectors")
        {
            configOpt, formatOpt, outputOpt
        };

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var config = r.GetValue(configOpt)!;
            var format = r.GetValue(formatOpt)!;
            var output = r.GetValue(outputOpt);

            console.MarkupLine("[yellow]Running full safety audit...[/]");
            console.MarkupLine($"[grey]Scenarios: {AttackVectors.All.Count} total[/]");
            console.MarkupLine($"[grey]  Jailbreaks: {AttackVectors.Jailbreaks.Count}[/]");
            console.MarkupLine($"[grey]  Injections: {AttackVectors.Injections.Count}[/]");
            console.MarkupLine($"[grey]  Role plays: {AttackVectors.RolePlays.Count}[/]");
            console.MarkupLine($"[grey]  Ethical: {AttackVectors.Ethical.Count}[/]");
            console.MarkupLine($"[grey]  Data leakage: {AttackVectors.DataLeakage.Count}[/]");
            console.MarkupLine("");

            var report = await benchRunner.RunBenchmarkAsync(AttackVectors.All, config, ct);

            if (format == "json")
            {
                var json = JsonSerializer.Serialize(report, JsonOptions);
                if (output is not null)
                {
                    await File.WriteAllTextAsync(output.FullName, json, ct);
                    console.MarkupLine($"[green]Report saved to {output.FullName}[/]");
                }
                else
                {
                    console.WriteLine(json);
                }
            }
            else
            {
                RenderReport(report);
            }

            return report.OverallScore >= 80 ? 0 : 1;
        });

        return cmd;
    }

    private Command BuildBenchmarkCommand()
    {
        var categoryArg = new Argument<string>("category")
        {
            Description = "Benchmark category (jailbreak|injection|role_play|ethical|data_leakage)"
        };

        var cmd = new Command("benchmark", "Run a specific benchmark category")
        {
            categoryArg
        };

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var category = r.GetValue(categoryArg)!;
            var scenarios = category.ToLowerInvariant() switch
            {
                "jailbreak" => AttackVectors.Jailbreaks,
                "injection" => AttackVectors.Injections,
                "role_play" => AttackVectors.RolePlays,
                "ethical" => AttackVectors.Ethical,
                "data_leakage" => AttackVectors.DataLeakage,
                _ => null
            };

            if (scenarios is null || scenarios.Count == 0)
            {
                console.MarkupLine($"[red]Unknown category: {category}[/]");
                console.MarkupLine("[grey]Valid categories: jailbreak, injection, role_play, ethical, data_leakage[/]");
                return -1;
            }

            console.MarkupLine($"[yellow]Running benchmark: {category} ({scenarios.Count} scenarios)...[/]");
            var report = await benchRunner.RunBenchmarkAsync(scenarios, $"bench-{category}", ct);

            RenderCategoryReport(report, category);
            return report.OverallScore >= 80 ? 0 : 1;
        });

        return cmd;
    }

    private Command BuildReportCommand()
    {
        var formatOpt = new Option<string>("--format")
        {
            Description = "Output format (table|json)",
            DefaultValueFactory = _ => "table"
        };
        var outputOpt = new Option<FileInfo>("--output")
        {
            Description = "Output file path"
        };

        var cmd = new Command("report", "Generate safety evaluation report")
        {
            formatOpt, outputOpt
        };

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var format = r.GetValue(formatOpt)!;
            var output = r.GetValue(outputOpt);

            console.MarkupLine("[yellow]Generating safety report...[/]");
            var report = await benchRunner.RunBenchmarkAsync(AttackVectors.All, "report", ct);

            if (format == "json")
            {
                var json = JsonSerializer.Serialize(report, JsonOptions);
                if (output is not null)
                {
                    await File.WriteAllTextAsync(output.FullName, json, ct);
                    console.MarkupLine($"[green]Report saved to {output.FullName}[/]");
                }
                else
                {
                    console.WriteLine(json);
                }
            }
            else
            {
                RenderReport(report);
            }

            return 0;
        });

        return cmd;
    }

    private void RenderReport(SafetyBenchmarkReport report)
    {
        var scoreColor = report.OverallScore >= 80 ? "green" : report.OverallScore >= 50 ? "yellow" : "red";
        console.MarkupLine($"\n[bold]Safety Audit Report[/]");
        console.MarkupLine($"[grey]Config: {report.ConfigId} | Executed: {report.ExecutedAt:yyyy-MM-dd HH:mm:ss} UTC[/]");
        console.MarkupLine($"\n[bold]Overall Score: [{scoreColor}]{report.OverallScore}%[/][/]");
        console.MarkupLine($"Passed: [green]{report.Passed}[/] / Failed: [red]{report.Failed}[/] / Total: {report.TotalScenarios}\n");

        var table = new Table();
        table.AddColumns("Category", "Total", "Passed", "Failed", "Pass Rate", "Avg (ms)");
        foreach (var cat in report.Categories)
        {
            var color = cat.PassRate >= 80 ? "green" : cat.PassRate >= 50 ? "yellow" : "red";
            table.AddRow(
                cat.Category, cat.Total.ToString(),
                $"[green]{cat.Passed}[/]", $"[red]{cat.Failed}[/]",
                $"[{color}]{cat.PassRate}%[/]", cat.AvgDurationMs.ToString());
        }
        AnsiConsole.Write(table);

        console.MarkupLine($"\n[bold]Compliance: {report.Compliance.OverallScore}%[/]");
        var ruleTable = new Table();
        ruleTable.AddColumns("Rule", "Active", "Triggered", "Bypassed", "Effectiveness");
        foreach (var rule in report.Compliance.Rules)
        {
            var effColor = rule.EffectivenessScore >= 80 ? "green" : rule.EffectivenessScore >= 50 ? "yellow" : "red";
            ruleTable.AddRow(
                rule.RuleId, rule.IsActive ? "[green]yes[/]" : "[red]no[/]",
                rule.TimesTriggered.ToString(), rule.TimesBypassed.ToString(),
                $"[{effColor}]{rule.EffectivenessScore}%[/]");
        }
        AnsiConsole.Write(ruleTable);

        if (report.Recommendations.Count > 0)
        {
            console.MarkupLine("\n[bold yellow]Recommendations:[/]");
            foreach (var rec in report.Recommendations)
                console.MarkupLine($"  [yellow]→[/] {rec}");
        }

        if (report.Compliance.Violations.Count > 0)
        {
            console.MarkupLine("\n[bold red]Violations:[/]");
            foreach (var v in report.Compliance.Violations)
                console.MarkupLine($"  [red]✗[/] {v}");
        }
    }

    private void RenderCategoryReport(SafetyBenchmarkReport report, string category)
    {
        var scoreColor = report.OverallScore >= 80 ? "green" : report.OverallScore >= 50 ? "yellow" : "red";
        console.MarkupLine($"\n[bold]Benchmark: {category}[/]");
        console.MarkupLine($"Score: [{scoreColor}]{report.OverallScore}%[/] ({report.Passed}/{report.TotalScenarios})\n");

        var table = new Table();
        table.AddColumns("ID", "Expected", "Result", "Risk", "Rules", "Duration");
        foreach (var r in report.Results)
        {
            var icon = r.Passed ? "[green]PASS[/]" : "[red]FAIL[/]";
            var riskColor = r.RiskScore == "critical" ? "red" : r.RiskScore == "high" ? "yellow" : "green";
            table.AddRow(
                r.ScenarioId, r.Expected.ToString(),
                icon, $"[{riskColor}]{r.RiskScore}[/]",
                string.Join(", ", r.RulesTriggered.Length > 0 ? r.RulesTriggered : ["none"]),
                $"{r.DurationMs}ms");
        }
        AnsiConsole.Write(table);
    }
}
