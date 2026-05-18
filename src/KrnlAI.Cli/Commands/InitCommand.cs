using System.CommandLine;
using KrnlAI.Cli.Abstractions;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class InitCommand(ITemplateEngine templateEngine, IAnsiConsole console)
{
    public Command Build()
    {
        var cmd = new Command("init", "Interactive project initialization");

        cmd.SetAction(async (ParseResult _, CancellationToken ct) =>
        {
            var name = console.Ask<string>("[cyan]Agent name:[/]");

            var safety = console.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan]Safety level:[/]")
                    .AddChoices("strict", "moderate", "relaxed"));

            var memory = console.Confirm("[cyan]Enable memory?[/]");

            var llm = console.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan]LLM provider:[/]")
                    .AddChoices("ollama", "openai", "azure", "openrouter"));

            var output = Path.Combine(Directory.GetCurrentDirectory(), name);

            if (Directory.Exists(output))
            {
                console.MarkupLine($"[red]Directory {output} already exists.[/]");
                return -1;
            }

            var vars = new Dictionary<string, string>
            {
                ["SafetyLevel"] = safety,
                ["LlmProvider"] = llm,
                ["MemoryEnabled"] = memory ? "true" : "false"
            };

            console.MarkupLine($"[green]Scaffolding agent '{name}'...[/]");
            await templateEngine.ScaffoldAsync(TemplateType.Agent, name, output, vars);

            console.MarkupLine($"[bold green]✅ Created {name}/[/]");
            console.MarkupLine($"[green]   Safety level: {safety}[/]");
            console.MarkupLine($"[green]   LLM provider: {llm}[/]");
            console.MarkupLine($"[green]   Memory: {(memory ? "enabled" : "disabled")}[/]");
            console.MarkupLine("");
            console.MarkupLine("[yellow]Next steps:[/]");
            console.MarkupLine($"  [cyan]cd {name}[/]");
            console.MarkupLine("  [cyan]dotnet restore[/]");
            console.MarkupLine("  [cyan]dotnet run[/]");
            console.MarkupLine("  [cyan]krnlai debug cycle[/]");

            return 0;
        });

        return cmd;
    }
}
