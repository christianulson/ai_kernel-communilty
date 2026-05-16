using System.CommandLine;
using AIKernel.Cli.Abstractions;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class NewCommand(ITemplateEngine templateEngine, IAnsiConsole console)
{
    public Command Build()
    {
        var cmd = new Command("new", "Scaffold new AI Kernel components");

        cmd.Add(BuildAgentCommand());
        cmd.Add(BuildToolCommand());
        cmd.Add(BuildPolicyCommand());
        cmd.Add(BuildCognitiveCycleCommand());

        return cmd;
    }

    private Command BuildAgentCommand()
    {
        var nameArg = new Argument<string>("name") { Description = "Agent name" };
        var outputOpt = new Option<DirectoryInfo>("--output")
        {
            Description = "Output directory",
            DefaultValueFactory = _ => new DirectoryInfo(".")
        };
        var safetyOpt = new Option<string>("--safety")
        {
            Description = "Safety level (strict|moderate|relaxed)",
            DefaultValueFactory = _ => "strict"
        };
        var llmOpt = new Option<string>("--llm")
        {
            Description = "LLM provider",
            DefaultValueFactory = _ => "ollama"
        };
        var memoryOpt = new Option<bool>("--memory")
        {
            Description = "Enable memory",
            DefaultValueFactory = _ => true
        };

        var cmd = new Command("agent", "Scaffold a new agent project")
        {
            nameArg, outputOpt, safetyOpt, llmOpt, memoryOpt
        };

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var name = r.GetValue(nameArg)!;
            var output = r.GetValue(outputOpt)!.FullName;
            var safety = r.GetValue(safetyOpt)!;
            var llm = r.GetValue(llmOpt)!;
            var memory = r.GetValue(memoryOpt);

            var targetDir = Path.Combine(output, name);

            if (Directory.Exists(targetDir))
            {
                console.MarkupLine($"[red]Directory {targetDir} already exists.[/]");
                return -1;
            }

            var vars = new Dictionary<string, string>
            {
                ["SafetyLevel"] = safety,
                ["LlmProvider"] = llm,
                ["MemoryEnabled"] = memory ? "true" : "false"
            };

            console.MarkupLine($"[green]Scaffolding agent '{name}'...[/]");
            await templateEngine.ScaffoldAsync(TemplateType.Agent, name, targetDir, vars);

            console.MarkupLine($"[bold green]✅ Created {name}/[/]");
            console.MarkupLine($"[green]   Safety level: {safety}[/]");
            console.MarkupLine($"[green]   LLM provider: {llm}[/]");
            console.MarkupLine($"[green]   Memory: {(memory ? "enabled" : "disabled")}[/]");
            console.MarkupLine("");
            console.MarkupLine("[yellow]Next steps:[/]");
            console.MarkupLine($"  [cyan]cd {name}[/]");
            console.MarkupLine("  [cyan]dotnet restore[/]");
            console.MarkupLine("  [cyan]dotnet run[/]");

            return 0;
        });

        return cmd;
    }

    private Command BuildToolCommand()
    {
        var nameArg = new Argument<string>("name") { Description = "Tool name" };
        var outputOpt = new Option<DirectoryInfo>("--output")
        {
            Description = "Output directory",
            DefaultValueFactory = _ => new DirectoryInfo(".")
        };

        var cmd = new Command("tool", "Scaffold a new tool")
        {
            nameArg, outputOpt
        };

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var name = r.GetValue(nameArg)!;
            var output = r.GetValue(outputOpt)!.FullName;

            console.MarkupLine($"[green]Scaffolding tool '{name}'...[/]");
            await templateEngine.ScaffoldAsync(TemplateType.Tool, name, output);
            console.MarkupLine($"[bold green]✅ Created {name}.cs in {output}[/]");
            return 0;
        });

        return cmd;
    }

    private Command BuildPolicyCommand()
    {
        var nameArg = new Argument<string>("name") { Description = "Policy name" };
        var outputOpt = new Option<DirectoryInfo>("--output")
        {
            Description = "Output directory",
            DefaultValueFactory = _ => new DirectoryInfo(".")
        };

        var cmd = new Command("policy", "Scaffold a new safety policy")
        {
            nameArg, outputOpt
        };

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var name = r.GetValue(nameArg)!;
            var output = r.GetValue(outputOpt)!.FullName;

            console.MarkupLine($"[green]Scaffolding policy '{name}'...[/]");
            await templateEngine.ScaffoldAsync(TemplateType.Policy, name, output);
            console.MarkupLine($"[bold green]✅ Created {name}.cs in {output}[/]");
            return 0;
        });

        return cmd;
    }

    private Command BuildCognitiveCycleCommand()
    {
        var nameArg = new Argument<string>("name") { Description = "Cycle name" };
        var outputOpt = new Option<DirectoryInfo>("--output")
        {
            Description = "Output directory",
            DefaultValueFactory = _ => new DirectoryInfo(".")
        };

        var cmd = new Command("cognitive-cycle", "Scaffold a custom cognitive cycle")
        {
            nameArg, outputOpt
        };

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var name = r.GetValue(nameArg)!;
            var output = r.GetValue(outputOpt)!.FullName;

            console.MarkupLine($"[green]Scaffolding cognitive cycle '{name}'...[/]");
            await templateEngine.ScaffoldAsync(TemplateType.CognitiveCycle, name, output);
            console.MarkupLine($"[bold green]✅ Created CognitiveCycle.cs in {output}[/]");
            return 0;
        });

        return cmd;
    }
}
