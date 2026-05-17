using System.CommandLine;
using System.Diagnostics;
using System.Text.Json;
using AIKernel.Cli.Tui;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class ExportCommand
{
    public Command Build()
    {
        var cmd = new Command("export", "Export data (sessions, config)");

        var sessionCmd = new Command("session", "Export a TUI session to JSON")
        {
            new Argument<string>("id") { Description = "Session ID to export. Use 'last' for the most recent." },
            new Option<string>("--output") { Description = "Output file path" },
        };
        sessionCmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var id = r.GetValue<string>("id") ?? "last";
            var output = r.GetValue<string>("--output");

            var store = new TuiSessionStore();
            string json;

            if (id == "last")
            {
                var sessions = await store.ListAsync();
                if (sessions.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]No sessions found.[/]");
                    return 1;
                }
                json = await store.ExportAsync(sessions[0].Id);
                AnsiConsole.MarkupLine($"[green]Exporting most recent session:[/] {sessions[0].Label}");
            }
            else
            {
                json = await store.ExportAsync(id);
                if (string.IsNullOrEmpty(json))
                {
                    AnsiConsole.MarkupLine($"[red]Session not found: {id}[/]");
                    return 1;
                }
            }

            if (!string.IsNullOrWhiteSpace(output))
            {
                await File.WriteAllTextAsync(output, json, ct);
                AnsiConsole.MarkupLine($"[green]Session exported to:[/] {output}");
            }
            else
            {
                AnsiConsole.MarkupLine("[bold]Exported Session:[/]");
                AnsiConsole.WriteLine(json);
            }
            return 0;
        });

        var configCmd = new Command("config", "Export current configuration")
        {
            new Option<string>("--output") { Description = "Output file path" },
        };
        configCmd.SetAction((ParseResult r, CancellationToken _) =>
        {
            var output = r.GetValue<string>("--output");
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var configDir = Path.Combine(home, ".aikernel");

            var config = new Dictionary<string, object>();
            if (Directory.Exists(configDir))
            {
                foreach (var file in Directory.GetFiles(configDir, "*.json"))
                {
                    try
                    {
                        var content = File.ReadAllText(file);
                        config[Path.GetFileName(file)] = content;
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning("Failed to read configuration file '{0}': {1}", file, ex.Message);
                    }
                }
            }

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

            if (!string.IsNullOrWhiteSpace(output))
            {
                File.WriteAllText(output, json);
                AnsiConsole.MarkupLine($"[green]Configuration exported to:[/] {output}");
            }
            else
            {
                AnsiConsole.MarkupLine("[bold]Exported Configuration:[/]");
                AnsiConsole.WriteLine(json);
            }
            return Task.FromResult(0);
        });

        cmd.Add(sessionCmd);
        cmd.Add(configCmd);
        return cmd;
    }
}
