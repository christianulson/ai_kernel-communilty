using System.CommandLine;
using KrnlAI.Cli.Abstractions;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class TemplatesCommand(ITemplateEngine templateEngine)
{
    public Command Build()
    {
        var cmd = new Command("templates", "List available scaffolding templates");

        cmd.SetAction(async (ParseResult _, CancellationToken ct) =>
        {
            var templates = await templateEngine.ListTemplatesAsync().ConfigureAwait(false);

            if (templates.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No templates available.[/]");
                return 0;
            }

            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumn("Type");
            table.AddColumn("Name");
            table.AddColumn("Description");
            table.AddColumn("Version");

            foreach (var t in templates.OrderBy(t => t.Type).ThenBy(t => t.Name))
            {
                table.AddRow(
                    t.Type.ToString(),
                    t.Name,
                    t.Description,
                    t.Version
                );
            }

            AnsiConsole.Write(table);

            AnsiConsole.MarkupLine("\n[grey]Usage: krnlai new <type> <name> [--template <name>][/]");
            AnsiConsole.MarkupLine("[grey]  e.g.: krnlai new agent my-agent --template coding-agent[/]");
            return 0;
        });

        return cmd;
    }
}
