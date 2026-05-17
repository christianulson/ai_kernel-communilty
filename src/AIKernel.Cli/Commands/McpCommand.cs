using System.CommandLine;
using AIKernel.Cli.Services;
using Kernel.Core.Abstractions.Mcp;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class McpCommand(CliContext ctx, ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("mcp", "Manage MCP servers");

        cmd.Add(BuildList());
        cmd.Add(BuildAdd());
        cmd.Add(BuildRemove());

        return cmd;
    }

    private Command BuildList()
    {
        var cmd = new Command("list", "List registered MCP servers");
        cmd.SetAction((ParseResult _, CancellationToken ct) =>
        {
            var servers = ctx.McpRegistry.GetAllServers();
            if (servers.Count == 0)
            {
                renderer.Console.MarkupLine("[yellow]No MCP servers registered[/]");
                return Task.FromResult(0);
            }
            var rows = servers.Select(s => new
            {
                s.ServerId,
                s.Name,
                Transport = s.TransportType.ToString(),
                Connected = s.IsConnected ? "yes" : "no"
            }).ToList();
            renderer.RenderTable(rows, "ServerId", "Name", "Transport", "Connected");
            return Task.FromResult(0);
        });
        return cmd;
    }

    private Command BuildAdd()
    {
        var nameArg = new Argument<string>("name") { Description = "Server name" };
        var commandOpt = new Option<string>("--command")
        {
            Description = "Command to start the MCP server",
            DefaultValueFactory = _ => ""
        };
        var argsOpt = new Option<string>("--args")
        {
            Description = "Command arguments",
            DefaultValueFactory = _ => ""
        };
        var typeOpt = new Option<string>("--type")
        {
            Description = "Transport type (stdio|sse)",
            DefaultValueFactory = _ => "stdio"
        };
        var urlOpt = new Option<string>("--url")
        {
            Description = "SSE endpoint URL (required for sse type)"
        };

        var cmd = new Command("add", "Register a new MCP server")
        {
            nameArg, commandOpt, argsOpt, typeOpt, urlOpt
        };

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var name = r.GetValue(nameArg)!;
            var command = r.GetValue(commandOpt)!;
            var args = r.GetValue(argsOpt)!;
            var typeStr = r.GetValue(typeOpt)!;
            var url = r.GetValue(urlOpt);

            if (!Enum.TryParse<McpTransportType>(typeStr, ignoreCase: true, out var transport))
            {
                renderer.Console.MarkupLine($"[red]Invalid transport type '{typeStr}'. Use stdio or sse.[/]");
                return 1;
            }

            if (transport == McpTransportType.Sse && string.IsNullOrWhiteSpace(url))
            {
                renderer.Console.MarkupLine("[red]SSE transport requires --url parameter[/]");
                return 1;
            }

            var serverId = name.ToLowerInvariant().Replace(" ", "-");
            var config = new McpServerConfig(
                ServerId: serverId,
                Name: name,
                TransportType: transport,
                Command: string.IsNullOrWhiteSpace(command) ? null : command,
                Args: string.IsNullOrWhiteSpace(args) ? null : args.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                Url: string.IsNullOrWhiteSpace(url) ? null : url);

            await ctx.McpRegistry.RegisterFromConfigAsync([config], ct);
            renderer.Console.MarkupLine($"[green]MCP server '{name}' registered[/]");
            return 0;
        });

        return cmd;
    }

    private Command BuildRemove()
    {
        var nameArg = new Argument<string>("name") { Description = "Server name or ID to remove" };
        var cmd = new Command("remove", "Remove an MCP server") { nameArg };

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var name = r.GetValue(nameArg)!;

            var server = ctx.McpRegistry.GetServer(name);
            if (server is null)
            {
                renderer.Console.MarkupLine($"[red]MCP server '{name}' not found[/]");
                return 1;
            }

            await ctx.McpRegistry.RemoveServerAsync(name, ct);
            renderer.Console.MarkupLine($"[green]MCP server '{name}' removed[/]");
            return 0;
        });

        return cmd;
    }
}
