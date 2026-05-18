using System.CommandLine;
using KrnlAI.Core.Config;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class ConfigCommand
{
    private readonly IAnsiConsole _console;

    private static readonly YamlConfigLoader Loader = new();

    public ConfigCommand(IAnsiConsole console)
    {
        _console = console;
    }

    public Command Build()
    {
        var cmd = new Command("config", "Manage declarative YAML configuration");

        cmd.Add(BuildValidate());
        cmd.Add(BuildShow());
        cmd.Add(BuildExport());

        return cmd;
    }

    private Command BuildValidate()
    {
        var fileArg = new Argument<string>("file") { Description = "Path to YAML config file" };
        var cmd = new Command("validate", "Validate a YAML config file") { fileArg };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var file = r.GetValue(fileArg)!;
            return await ValidateAsync(file, ct);
        });
        return cmd;
    }

    private Command BuildShow()
    {
        var cmd = new Command("show", "Show current effective configuration");
        cmd.SetAction(async (ParseResult _, CancellationToken ct) =>
        {
            return await ShowAsync(ct);
        });
        return cmd;
    }

    private Command BuildExport()
    {
        var nameArg = new Argument<string>("name") { Description = "Configuration name" };
        var formatOpt = new Option<string>("--format")
        {
            Description = "Output format",
            DefaultValueFactory = _ => "yaml"
        };
        var cmd = new Command("export", "Export configuration to YAML") { nameArg, formatOpt };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var name = r.GetValue(nameArg)!;
            return await ExportAsync(name, ct);
        });
        return cmd;
    }

    private async Task<int> ValidateAsync(string file, CancellationToken ct)
    {
        if (!File.Exists(file))
        {
            _console.MarkupLine("[red]File not found:[/] {0}", file);
            return 1;
        }

        try
        {
            var config = await Loader.LoadAsync(file);
            _console.MarkupLine("[green]Valid configuration:[/]");
            _console.MarkupLine("  Name: {0}", config.Name);
            _console.MarkupLine("  Version: {0}", config.Version);
            _console.MarkupLine("  Environment: {0}", config.Environment);
            _console.MarkupLine("  Cognitive steps: {0}", string.Join(", ", config.CognitiveSteps));
            _console.MarkupLine("  Safety level: {0}", config.SafetyLevel);
            _console.MarkupLine("  Tools: {0}", string.Join(", ", config.ToolAllowlist));
            return 0;
        }
        catch (Exception ex)
        {
            _console.MarkupLine("[red]Validation failed:[/] {0}", ex.Message);
            return 1;
        }
    }

    private Task<int> ShowAsync(CancellationToken ct)
    {
        _console.MarkupLine("[yellow]Current configuration:[/]");
        _console.MarkupLine("  Use [bold]krnlai config validate <file>[/] to validate a YAML file.");
        _console.MarkupLine("  Use [bold]krnlai run --config <file>[/] to run with a specific config.");
        return Task.FromResult(0);
    }

    private async Task<int> ExportAsync(string name, CancellationToken ct)
    {
        var yaml = $"""
            apiVersion: krnlai.io/v1
            kind: CognitiveSystem
            metadata:
              name: {name}
              version: 1.0.0
              environment: production
            cognitive_cycle:
              mode: adaptive
              steps:
                - sensor
                - attention
                - evaluation
                - metacognition
                - planning
                - governance
                - execution
              timeout: 30s
              max_iterations: 10
            safety:
              level: strict
              rules: [R01, R02, R03, R05, R10]
              require_approval: true
              risk_threshold: 0.7
            """;

        var fileName = $"{name}.yaml";
        await File.WriteAllTextAsync(fileName, yaml, ct);
        _console.MarkupLine("[green]Exported configuration to:[/] {0}", fileName);
        return 0;
    }
}
