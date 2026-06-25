using System.CommandLine;
using System.Text.Json;
using KrnlAI.Cli.Services;
using KrnlAI.Cognition.Contracts;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

internal sealed class SessionCommand(
    IAnsiConsole console,
    InMemorySessionStore sessionStore,
    ISessionStore cognitiveStore)
{
    public Command Build()
    {
        var cmd = new Command("session", "Manage CLI sessions")
        {
            BuildList(),
            BuildCreate(),
            BuildExport(),
            BuildImport(),
            BuildDelete(),
            BuildFork(),
            BuildResume(),
            BuildShow(),
            BuildTimeline()
        };

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

    private Command BuildFork()
    {
        var idArg = new Argument<string>("id") { Description = "Cognitive session ID to fork" };
        var cmd = new Command("fork", "Fork a cognitive session") { idArg };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var id = r.GetValue(idArg)!;
            try
            {
                var fork = await cognitiveStore.ForkAsync(id, ct: ct).ConfigureAwait(false);
                console.MarkupLine($"[green]Session forked:[/] {fork.SessionId}");
                console.MarkupLine($"  Forked from: {fork.ForkedFrom}");
                console.MarkupLine($"  Fork depth: {fork.ForkDepth}");
                console.MarkupLine($"  Cycles: {fork.CycleIds.Count}");
                return 0;
            }
            catch (KeyNotFoundException)
            {
                console.MarkupLine($"[red]Session '{id}' not found[/]");
                return 1;
            }
        });
        return cmd;
    }

    private Command BuildResume()
    {
        var idArg = new Argument<string>("id") { Description = "Cognitive session ID to resume" };
        var cmd = new Command("resume", "Resume a cognitive session") { idArg };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var id = r.GetValue(idArg)!;
            var ctx = await cognitiveStore.ResumeAsync(id, ct).ConfigureAwait(false);
            if (ctx is null)
            {
                console.MarkupLine($"[yellow]Session '{id}' has no saved context to resume[/]");
                return 1;
            }
            console.MarkupLine($"[green]Session resumed:[/] {id}");
            console.MarkupLine($"  Goal: {ctx.Goal}");
            console.MarkupLine($"  Domain: {ctx.Domain}");
            console.MarkupLine($"  Cycle: {ctx.CycleId}");
            return 0;
        });
        return cmd;
    }

    private Command BuildShow()
    {
        var idArg = new Argument<string>("id") { Description = "Cognitive session ID" };
        var cmd = new Command("show", "Show session details") { idArg };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var id = r.GetValue(idArg)!;
            var session = await cognitiveStore.GetAsync(id, ct).ConfigureAwait(false);
            if (session is null)
            {
                console.MarkupLine($"[red]Session '{id}' not found[/]");
                return 1;
            }

            var table = new Table();
            table.AddColumns("Property", "Value");
            table.AddRow("Session ID", session.SessionId);
            table.AddRow("Status", session.Status.ToString());
            table.AddRow("Started At", session.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            table.AddRow("Completed At", session.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-");
            table.AddRow("Fork Depth", session.ForkDepth.ToString());
            table.AddRow("Forked From", session.ForkedFrom ?? "(original)");
            table.AddRow("Cycles", session.CycleIds.Count.ToString());
            table.AddRow("Summary", session.Summary);
            console.Write(table);
            return 0;
        });
        return cmd;
    }

    private Command BuildTimeline()
    {
        var idArg = new Argument<string>("id") { Description = "Cognitive session ID" };
        var cmd = new Command("timeline", "Show session fork timeline") { idArg };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var id = r.GetValue(idArg)!;
            var session = await cognitiveStore.GetAsync(id, ct).ConfigureAwait(false);
            if (session is null)
            {
                console.MarkupLine($"[red]Session '{id}' not found[/]");
                return 1;
            }

            var table = new Table();
            table.AddColumns("Property", "Value");
            table.AddRow("Session ID", session.SessionId);
            table.AddRow("Status", session.Status.ToString());
            table.AddRow("Fork Depth", session.ForkDepth.ToString());
            table.AddRow("Forked From", session.ForkedFrom ?? "(original)");
            table.AddRow("Cycles", session.CycleIds.Count.ToString());
            table.AddRow("Summary", session.Summary);
            console.Write(table);

            if (!string.IsNullOrWhiteSpace(session.ForkedFrom))
            {
                console.MarkupLine($"\n[yellow]Fork chain:[/] {session.ForkedFrom} → {session.SessionId}");
            }
            return 0;
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
