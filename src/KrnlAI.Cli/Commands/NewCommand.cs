using System.CommandLine;
using KrnlAI.Cli.Abstractions;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class NewCommand(ITemplateEngine templateEngine, IAnsiConsole console)
{
    public Command Build()
    {
        var cmd = new Command("new", "Scaffold new Krnl-AI components");

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
        var templateOpt = new Option<string>("--template")
        {
            Description = "Template variant (basic|coding-agent)",
            DefaultValueFactory = _ => "basic"
        };

        var cmd = new Command("agent", "Scaffold a new agent project")
        {
            nameArg, outputOpt, safetyOpt, llmOpt, memoryOpt, templateOpt
        };

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var name = r.GetValue(nameArg)!;
            var output = r.GetValue(outputOpt)!.FullName;
            var safety = r.GetValue(safetyOpt)!;
            var llm = r.GetValue(llmOpt)!;
            var memory = r.GetValue(memoryOpt);
            var template = r.GetValue(templateOpt) ?? "basic";

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
                ["MemoryEnabled"] = memory ? "true" : "false",
                ["TemplateVersion"] = "1.0.0",
                ["AgentGoal"] = "Accomplish tasks autonomously",
                ["TemplateName"] = template,
            };

            console.MarkupLine($"[green]Scaffolding agent '{name}' with template '{template}'...[/]");
            await templateEngine.ScaffoldAsync(TemplateType.Agent, name, targetDir, vars);

            console.MarkupLine($"[bold green]✅ Created {name}/[/]");
            console.MarkupLine($"[green]   Template: {template}[/]");
            console.MarkupLine($"[green]   Safety level: {safety}[/]");
            console.MarkupLine($"[green]   LLM provider: {llm}[/]");
            console.MarkupLine($"[green]   Memory: {(memory ? "enabled" : "disabled")}[/]");
            console.MarkupLine("");
            console.MarkupLine("[yellow]Next steps:[/]");
            console.MarkupLine($"  [cyan]cd {name}[/]");
            console.MarkupLine("  [cyan]dotnet restore[/]");
            console.MarkupLine("  [cyan]dotnet run[/]");
            console.MarkupLine("  [cyan]krnlai templates[/]");
            if (template == "coding-agent")
                console.MarkupLine("  [cyan]krnlai debug cycle[/]");

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
