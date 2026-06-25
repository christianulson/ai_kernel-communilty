using System.CommandLine;
using KrnlAI.Core.Config;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class ConfigCommand(IAnsiConsole console, ManagedSettingsChain? managedChain = null)
{
    private readonly IAnsiConsole _console = console;
    private static readonly YamlConfigLoader Loader = new();

    public Command Build()
    {
        var cmd = new Command("config", "Manage declarative YAML configuration")
        {
            BuildValidate(),
            BuildShow(),
            BuildExport(),
            BuildManaged()
        };

        return cmd;
    }

    private Command BuildManaged()
    {
        var managedCmd = new Command("managed", "Manage managed/enterprise settings");

        var listCmd = new Command("list", "List managed settings with sources");
        listCmd.SetAction(async (_, ct) =>
        {
            if (managedChain is null)
            {
                _console.MarkupLine("[yellow]Managed settings chain not available[/]");
                return 0;
            }

            var settings = managedChain.Current ?? await managedChain.BuildAsync(ct).ConfigureAwait(false);
            var all = settings.All();
            if (all.Count == 0)
            {
                _console.MarkupLine("[yellow]No managed settings loaded[/]");
                return 0;
            }

            var table = new Table();
            table.AddColumns("Setting", "Value", "Source", "Managed");
            foreach (var (key, setting) in all)
                table.AddRow(key, setting.Value?.ToString() ?? "(null)", setting.Source, setting.IsManaged ? "✅" : "❌");
            _console.Write(table);
            return 0;
        });

        var auditCmd = new Command("audit", "Audit managed settings compliance");
        auditCmd.SetAction(async (_, ct) =>
        {
            if (managedChain is null)
            {
                _console.MarkupLine("[yellow]Managed settings chain not available[/]");
                return 0;
            }

            var settings = managedChain.Current ?? await managedChain.BuildAsync(ct).ConfigureAwait(false);
            var validator = new ManagedSettingsValidator();
            var result = validator.Validate(settings);

            if (result.IsValid)
            {
                _console.MarkupLine("[green]All settings compliant[/]");
            }
            else
            {
                _console.MarkupLine("[red]Validation issues found:[/]");
                foreach (var warning in result.Warnings)
                    _console.MarkupLine($"  [yellow]⚠[/] {warning}");
            }
            return 0;
        });

        var checkArg = new Argument<string>("setting") { Description = "Setting path (e.g. RateLimiting:MaxRequests)" };
        var checkCmd = new Command("check", "Check effective value of a setting") { checkArg };
        checkCmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            if (managedChain is null)
            {
                _console.MarkupLine("[yellow]Managed settings chain not available[/]");
                return 1;
            }

            var settingKey = r.GetValue(checkArg)!;
            var settings = managedChain.Current ?? await managedChain.BuildAsync(ct).ConfigureAwait(false);
            var setting = settings.Get(settingKey);

            if (setting is null)
            {
                _console.MarkupLine($"[yellow]Setting '{settingKey}' not found[/]");
                return 1;
            }

            _console.MarkupLine($"[green]{settingKey}[/]");
            _console.MarkupLine($"  Value:   {setting.Value}");
            _console.MarkupLine($"  Source:  {setting.Source}");
            _console.MarkupLine($"  Managed: {(setting.IsManaged ? "✅ Yes" : "❌ No")}");
            if (setting.PolicyId is not null)
                _console.MarkupLine($"  Policy:  {setting.PolicyId}");
            return 0;
        });

        managedCmd.Add(listCmd);
        managedCmd.Add(auditCmd);
        managedCmd.Add(checkCmd);
        return managedCmd;
    }

    private Command BuildValidate()
    {
        var fileArg = new Argument<string>("file") { Description = "Path to YAML config file" };
        var cmd = new Command("validate", "Validate a YAML config file") { fileArg };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var file = r.GetValue(fileArg)!;
            return await ValidateAsync(file, ct).ConfigureAwait(false);
        });
        return cmd;
    }

    private Command BuildShow()
    {
        var cmd = new Command("show", "Show current effective configuration");
        cmd.SetAction(async (ParseResult _, CancellationToken ct) =>
        {
            return await ShowAsync(ct).ConfigureAwait(false);
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
            return await ExportAsync(name, ct).ConfigureAwait(false);
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
            var config = await Loader.LoadAsync(file).ConfigureAwait(false);
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
        await File.WriteAllTextAsync(fileName, yaml, ct).ConfigureAwait(false);
        _console.MarkupLine("[green]Exported configuration to:[/] {0}", fileName);
        return 0;
    }
}
