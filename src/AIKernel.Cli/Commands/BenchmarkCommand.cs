using System.CommandLine;
using System.Text.Json;
using AIKernel.Cli.Abstractions;
using AIKernel.Cli.Services;
using Kernel.Core.Abstractions.Safety;
using Kernel.Core.Model;
using Kernel.Core.Services.Safety;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class BenchmarkCommand(
    SafetyBenchRunner benchRunner,
    ISafetyReportGenerator reportGenerator,
    IAnsiConsole console)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public Command Build()
    {
        var cmd = new Command("benchmark", "Run safety benchmarks and comparisons");

        cmd.Add(BuildSafetyCommand());
        cmd.Add(BuildListCommand());

        return cmd;
    }

    private Command BuildSafetyCommand()
    {
        var competitorsOpt = new Option<string>("--competitors")
        {
            Description = "Competitors to compare against (comma-separated: openai,anthropic)"
        };
        var categoryOpt = new Option<string>("--category")
        {
            Description = "Specific category to benchmark (default: all)"
        };
        var outputOpt = new Option<string>("--output")
        {
            Description = "Output file path (saves HTML report)"
        };
        var seedOpt = new Option<int>("--seed")
        {
            Description = "Random seed for reproducibility",
            DefaultValueFactory = _ => 42
        };

        var safetyCmd = new Command("safety", "Run safety benchmark with optional competitor comparison")
        {
            competitorsOpt, categoryOpt, outputOpt, seedOpt
        };

        safetyCmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var competitors = r.GetValue(competitorsOpt);
            var category = r.GetValue(categoryOpt);
            var output = r.GetValue(outputOpt);
            var seed = r.GetValue(seedOpt);

            var scenarioList = !string.IsNullOrEmpty(category)
                ? category.ToLowerInvariant() switch
                {
                    "jailbreak" => AttackVectors.Jailbreaks,
                    "injection" => AttackVectors.Injections,
                    "role_play" => AttackVectors.RolePlays,
                    "ethical" => AttackVectors.Ethical,
                    "data_leakage" => AttackVectors.DataLeakage,
                    "action_safety" => AttackVectors.ActionSafety,
                    "context_manipulation" => AttackVectors.ContextManipulation,
                    _ => null
                }
                : AttackVectors.All;

            if (scenarioList is null || scenarioList.Count == 0)
            {
                console.MarkupLine("[red]Unknown category or no scenarios found[/]");
                return 1;
            }

            console.MarkupLine("[yellow]Running AI Kernel safety benchmark...[/]");
            console.MarkupLine($"[grey]Scenarios: {scenarioList.Count} | Seed: {seed}[/]");

            var result = await benchRunner.RunBenchmarkAsync(scenarioList, $"benchmark-{seed}", ct);

            var competitorsList = (competitors ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var externalResults = new Dictionary<string, object>();

            foreach (var comp in competitorsList)
            {
                IExternalSafetyEvaluator? evaluator = comp.ToLowerInvariant() switch
                {
                    "openai" => new OpenAiSafetyEvaluator(),
                    "anthropic" => new AnthropicSafetyEvaluator(),
                    _ => null
                };

                if (evaluator is null)
                {
                    console.MarkupLine($"[yellow]Unknown competitor: {comp}[/]");
                    continue;
                }

                console.MarkupLine($"[blue]Evaluating with {evaluator.Name}...[/]");
                var compResults = new List<object>();
                var blockedCount = 0;
                var totalTime = 0L;

                foreach (var scenario in scenarioList)
                {
                    var eval = await evaluator.EvaluateAsync(scenario.Prompt, scenario.Id, ct);
                    compResults.Add(new
                    {
                        scenarioId = eval.ScenarioId,
                        blocked = eval.Blocked,
                        riskLevel = eval.RiskLevel,
                        durationMs = eval.DurationMs
                    });
                    if (eval.Blocked) blockedCount++;
                    totalTime += eval.DurationMs;
                }

                externalResults[comp] = new
                {
                    evaluator = evaluator.Name,
                    totalScenarios = scenarioList.Count,
                    blocked = blockedCount,
                    passRate = scenarioList.Count > 0
                        ? Math.Round((double)blockedCount / scenarioList.Count * 100, 1)
                        : 0.0,
                    avgDurationMs = scenarioList.Count > 0 ? totalTime / scenarioList.Count : 0,
                    results = compResults
                };
            }

            var report = new
            {
                aikernel = new
                {
                    overallScore = result.OverallScore,
                    passed = result.Passed,
                    failed = result.Failed,
                    total = result.TotalScenarios,
                    categories = result.Categories.Select(c => new
                    {
                        c.Category, c.Total, c.Passed, c.Failed, c.PassRate
                    })
                },
                competitors = externalResults,
                recommendations = result.Recommendations
            };

            var json = JsonSerializer.Serialize(report, JsonOpts);

            if (!string.IsNullOrEmpty(output))
            {
                var html = reportGenerator.Generate(result, ReportFormat.Html);
                await File.WriteAllTextAsync(output, html, ct);
                console.MarkupLine($"[green]HTML report saved to: {output}[/]");
            }

            console.MarkupLine($"\n[bold]Results:[/]");
            console.MarkupLine($"  [green]AI Kernel:[/] {result.OverallScore}% ({result.Passed}/{result.TotalScenarios})");

            foreach (var (name, data) in externalResults)
            {
                var d = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(data, JsonOpts));
                var rate = d.GetProperty("passRate").GetDouble();
                var blocked = d.GetProperty("blocked").GetInt32();
                var total = d.GetProperty("totalScenarios").GetInt32();
                var color = rate >= 80 ? "green" : rate >= 50 ? "yellow" : "red";
                console.MarkupLine($"  [{color}]{d.GetProperty("evaluator").GetString()}:[/] {rate}% ({blocked}/{total})");
            }

            return 0;
        });

        return safetyCmd;
    }

    private Command BuildListCommand()
    {
        var cmd = new Command("list", "List available benchmark categories and scenarios");
        cmd.SetAction((ParseResult _, CancellationToken _) =>
        {
            var table = new Table();
            table.AddColumns("Category", "Count", "Description");
            table.AddRow("jailbreak", AttackVectors.Jailbreaks.Count.ToString(), "Prompt jailbreak attempts");
            table.AddRow("injection", AttackVectors.Injections.Count.ToString(), "Command/prompt injection");
            table.AddRow("role_play", AttackVectors.RolePlays.Count.ToString(), "Role-playing bypass");
            table.AddRow("ethical", AttackVectors.Ethical.Count.ToString(), "Ethical boundary tests");
            table.AddRow("data_leakage", AttackVectors.DataLeakage.Count.ToString(), "Data exfiltration attempts");
            table.AddRow("action_safety", AttackVectors.ActionSafety.Count.ToString(), "Unsafe action prevention");
            table.AddRow("context_manipulation", AttackVectors.ContextManipulation.Count.ToString(), "Context manipulation");
            table.AddRow("", AttackVectors.All.Count.ToString(), "[bold]TOTAL[/]");
            console.Write(table);
            return Task.FromResult(0);
        });
        return cmd;
    }
}