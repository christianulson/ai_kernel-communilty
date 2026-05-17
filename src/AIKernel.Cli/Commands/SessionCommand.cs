using System.CommandLine;
using System.Text.Json;
using AIKernel.Cli.Services;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class SessionCommand(IAnsiConsole console, InMemorySessionStore sessionStore)
{
    public Command Build()
    {
        var cmd = new Command("session", "Manage CLI sessions");

        cmd.Add(BuildList());
        cmd.Add(BuildCreate());
        cmd.Add(BuildExport());
        cmd.Add(BuildImport());
        cmd.Add(BuildDelete());

        return cmd;
    }

    private Command BuildList()
    {
        var cmd = new Command("list", "List sessions");
        cmd.SetAction((ParseResult _, CancellationToken _) =>
        {
            var sessions = sessionStore.List();
            if (sessions.Count == 0)
            {
                console.MarkupLine("[yellow]No sessions[/]");
                return Task.FromResult(0);
            }
            var table = new Table();
            table.AddColumns("Id", "Name", "Created");
            foreach (var s in sessions)
                table.AddRow(s.Id, s.Name, s.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
            console.Write(table);
            return Task.FromResult(0);
        });
        return cmd;
    }

    private Command BuildCreate()
    {
        var nameArg = new Argument<string>("name") { Description = "Session name" };
        var cmd = new Command("create", "Create a new session") { nameArg };
        cmd.SetAction((ParseResult r, CancellationToken _) =>
        {
            var name = r.GetValue(nameArg)!;
            var session = sessionStore.Create(name);
            console.MarkupLine($"[green]Session created:[/] {session.Id}");
            console.MarkupLine($"  Name: {session.Name}");
            console.MarkupLine($"  Created: {session.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            return Task.FromResult(0);
        });
        return cmd;
    }

    private Command BuildExport()
    {
        var idArg = new Argument<string>("id") { Description = "Session ID" };
        var outputOpt = new Option<FileInfo>("--output")
        {
            Description = "Output file path (default: stdout)"
        };
        var cmd = new Command("export", "Export session as JSON") { idArg, outputOpt };
        cmd.SetAction((ParseResult r, CancellationToken ct) =>
        {
            var id = r.GetValue(idArg)!;
            var output = r.GetValue(outputOpt);

            try
            {
                var json = sessionStore.ExportJson(id);
                if (output is not null)
                {
                    File.WriteAllText(output.FullName, json, System.Text.Encoding.UTF8);
                    console.MarkupLine($"[green]Session exported to:[/] {output.FullName}");
                }
                else
                {
                    console.WriteLine(json);
                }
                return Task.FromResult(0);
            }
            catch (KeyNotFoundException)
            {
                console.MarkupLine($"[red]Session '{id}' not found[/]");
                return Task.FromResult(1);
            }
        });
        return cmd;
    }

    private Command BuildImport()
    {
        var fileArg = new Argument<FileInfo>("file") { Description = "JSON file to import" };
        var cmd = new Command("import", "Import session from JSON") { fileArg };
        cmd.SetAction((ParseResult r, CancellationToken ct) =>
        {
            var file = r.GetValue(fileArg)!;
            if (!file.Exists)
            {
                console.MarkupLine($"[red]File not found:[/] {file.FullName}");
                return Task.FromResult(1);
            }

            try
            {
                var json = File.ReadAllText(file.FullName, System.Text.Encoding.UTF8);
                var session = sessionStore.ImportJson(json);
                console.MarkupLine($"[green]Session imported:[/] {session.Id}");
                console.MarkupLine($"  Name: {session.Name}");
                return Task.FromResult(0);
            }
            catch (JsonException ex)
            {
                console.MarkupLine($"[red]Invalid JSON:[/] {ex.Message}");
                return Task.FromResult(1);
            }
        });
        return cmd;
    }

    private Command BuildDelete()
    {
        var idArg = new Argument<string>("id") { Description = "Session ID" };
        var cmd = new Command("delete", "Delete a session") { idArg };
        cmd.SetAction((ParseResult r, CancellationToken _) =>
        {
            var id = r.GetValue(idArg)!;
            if (sessionStore.Delete(id))
            {
                console.MarkupLine($"[green]Session deleted:[/] {id}");
                return Task.FromResult(0);
            }
            console.MarkupLine($"[red]Session '{id}' not found[/]");
            return Task.FromResult(1);
        });
        return cmd;
    }
}
