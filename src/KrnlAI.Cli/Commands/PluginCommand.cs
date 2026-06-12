using System.CommandLine;
using System.Net.Http.Json;
using System.Text.Json;
using KrnlAI.Core.Abstractions;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class PluginCommand(
    IAnsiConsole console,
    IAssemblyPluginLoader? localLoader = null,
    IPluginCatalog? catalog = null,
    IPluginRegistryService? registry = null)
{
    private readonly List<string> _localPluginIds = new();

    public Command Build()
    {
        var cmd = new Command("plugin", "Manage plugins");

        cmd.Add(BuildInstall());
        cmd.Add(BuildList());
        cmd.Add(BuildRemove());
        cmd.Add(BuildSearch());
        cmd.Add(BuildInfo());
        cmd.Add(BuildRegistry());

        return cmd;
    }

    private Command BuildInstall()
    {
        var pathArg = new Argument<string>("path") { Description = "Plugin path or spec URL" };
        var endpointOpt = new Option<string>("--endpoint")
        {
            Description = "KrnlAI API endpoint (remote mode)",
            DefaultValueFactory = _ => "http://localhost:5000"
        };
        var typeOpt = new Option<string>("--type")
        {
            Description = "Plugin type: dotnet-assembly, openapi, mcp, script, executable",
            DefaultValueFactory = _ => "dotnet-assembly"
        };
        var localOpt = new Option<bool>("--local")
        {
            Description = "Load plugin locally (standalone mode)"
        };

        var cmd = new Command("install", "Install a plugin")
        {
            pathArg, endpointOpt, typeOpt, localOpt
        };

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var path = r.GetValue(pathArg)!;
            var local = r.GetValue(localOpt);

            if (local || localLoader is null)
                return await InstallLocalAsync(path, r.GetValue(typeOpt)!, ct);
            return await InstallRemoteAsync(path, r.GetValue(endpointOpt)!, ct);
        });

        return cmd;
    }

    private Command BuildList()
    {
        var cmd = new Command("list", "List installed plugins");
        cmd.SetAction((ParseResult _, CancellationToken _) =>
        {
            if (localLoader is not null)
            {
                console.MarkupLine("[yellow]Use 'krnlai plugin install <path> --local' to install plugins[/]");
            }
            else
            {
                console.MarkupLine("[yellow]Plugin loader not available[/]");
            }
            return Task.FromResult(0);
        });
        return cmd;
    }

    private Command BuildRemove()
    {
        var idArg = new Argument<string>("id") { Description = "Plugin ID to remove" };
        var endpointOpt = new Option<string>("--endpoint")
        {
            Description = "KrnlAI API endpoint (remote mode)",
            DefaultValueFactory = _ => "http://localhost:5000"
        };
        var localOpt = new Option<bool>("--local")
        {
            Description = "Remove plugin locally"
        };
        var cmd = new Command("remove", "Remove a plugin") { idArg, endpointOpt, localOpt };

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var id = r.GetValue(idArg)!;
            var local = r.GetValue(localOpt);

            if (local || localLoader is null)
            {
                _localPluginIds.Remove(id);
                console.MarkupLine($"[green]Plugin '{id}' removed locally[/]");
                return 0;
            }
            return await RemoveRemoteAsync(id, r.GetValue(endpointOpt)!, console, ct);
        });

        return cmd;
    }

    private Command BuildSearch()
    {
        var queryArg = new Argument<string>("query") { Description = "Search query" };
        var cmd = new Command("search", "Search plugin marketplace") { queryArg };

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            if (catalog is null)
            {
                console.MarkupLine("[yellow]Plugin catalog not available[/]");
                return 1;
            }

            var query = r.GetValue(queryArg)!;
            var result = await catalog.SearchAsync(query, ct);

            if (result.Results.Count == 0)
            {
                console.MarkupLine($"[yellow]No plugins found matching '{query}'[/]");
                return 0;
            }

            var table = new Table();
            table.AddColumns("Id", "Name", "Version", "Author", "Verified");
            foreach (var entry in result.Results)
                table.AddRow(entry.Id, entry.Name, entry.Version, entry.Author, entry.Verified ? "✅" : "❌");
            console.Write(table);
            console.MarkupLine($"[blue]{result.TotalCount} result(s)[/]");
            return 0;
        });

        return cmd;
    }

    private Command BuildInfo()
    {
        var idArg = new Argument<string>("id") { Description = "Plugin ID" };
        var cmd = new Command("info", "Show plugin details") { idArg };

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            if (catalog is null)
            {
                console.MarkupLine("[yellow]Plugin catalog not available[/]");
                return 1;
            }

            var id = r.GetValue(idArg)!;
            var entry = await catalog.GetByIdAsync(id, ct);

            if (entry is null)
            {
                console.MarkupLine($"[yellow]Plugin '{id}' not found[/]");
                return 0;
            }

            var panel = new Panel(
                Align.Left(new Markup(
                    $"[bold]Id:[/] {entry.Id}\n" +
                    $"[bold]Name:[/] {entry.Name}\n" +
                    $"[bold]Version:[/] {entry.Version}\n" +
                    $"[bold]Author:[/] {entry.Author}\n" +
                    $"[bold]Description:[/] {entry.Description}\n" +
                    $"[bold]Tags:[/] {string.Join(", ", entry.Tags)}\n" +
                    $"[bold]Downloads:[/] {entry.Downloads}\n" +
                    $"[bold]Verified:[/] {(entry.Verified ? "✅ Yes" : "❌ No")}\n" +
                    $"[bold]Published:[/] {entry.PublishedAt:yyyy-MM-dd}")))
            {
                Header = new PanelHeader($"[bold yellow]Plugin: {entry.Name}[/]"),
                Border = BoxBorder.Rounded
            };
            console.Write(panel);
            return 0;
        });

        return cmd;
    }

    private Command BuildRegistry()
    {
        var regCmd = new Command("registry", "Manage plugin registries");

        var listCmd = new Command("list", "List configured registries");
        listCmd.SetAction(async (_, ct) =>
        {
            if (registry is null)
            {
                console.MarkupLine("[yellow]Plugin registry service not available[/]");
                return 1;
            }

            var registries = await registry.ListRegistriesAsync(ct);
            if (registries.Count == 0)
            {
                console.MarkupLine("[yellow]No registries configured[/]");
                return 0;
            }

            var table = new Table();
            table.AddColumns("Id", "Url", "Enabled");
            foreach (var r in registries)
                table.AddRow(r.Id, r.Url, r.Enabled ? "✅" : "❌");
            console.Write(table);
            return 0;
        });

        var urlArg = new Argument<string>("url") { Description = "Registry URL" };
        var idArg = new Argument<string>("id") { Description = "Registry ID" };
        var addCmd = new Command("add", "Add a plugin registry") { idArg, urlArg };
        addCmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            if (registry is null)
            {
                console.MarkupLine("[yellow]Plugin registry service not available[/]");
                return 1;
            }

            var id = r.GetValue(idArg)!;
            var url = r.GetValue(urlArg)!;
            await registry.AddRegistryAsync(new PluginRegistryConfig(id, url), ct);
            console.MarkupLine($"[green]Registry '{id}' added[/]");
            return 0;
        });

        var removeCmd = new Command("remove", "Remove a plugin registry") { idArg };
        removeCmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            if (registry is null)
            {
                console.MarkupLine("[yellow]Plugin registry service not available[/]");
                return 1;
            }

            var id = r.GetValue(idArg)!;
            await registry.RemoveRegistryAsync(id, ct);
            console.MarkupLine($"[green]Registry '{id}' removed[/]");
            return 0;
        });

        regCmd.Add(listCmd);
        regCmd.Add(addCmd);
        regCmd.Add(removeCmd);
        return regCmd;
    }

    private Task<int> InstallLocalAsync(string path, string type, CancellationToken ct)
    {
        console.MarkupLine($"[yellow]Installing plugin (local mode):[/] {path}");
        _localPluginIds.Add(path);
        console.MarkupLine($"[green]Plugin '{path}' registered locally[/]");
        return Task.FromResult(0);
    }

    private static async Task<int> InstallRemoteAsync(string path, string endpoint, CancellationToken ct)
    {
        using var client = new HttpClient { BaseAddress = new Uri(endpoint.TrimEnd('/')) };

        AnsiConsole.MarkupLine($"[yellow]Installing plugin:[/] {path}");

        try
        {
            var manifest = new { kind = "plugin", name = path, version = "1.0", type = MapType(path) };
            var response = await client.PostAsJsonAsync("/admin/plugins", manifest, ct);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
                var id = result.TryGetProperty("id", out var idProp) ? idProp.GetString() : path;
                AnsiConsole.MarkupLine($"[green]Plugin '{id}' installed successfully![/]");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                AnsiConsole.MarkupLine($"[red]Failed to install plugin:[/] {error}");
                return 1;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }

        return 0;
    }

    private static async Task<int> RemoveRemoteAsync(string pluginId, string endpoint, IAnsiConsole console, CancellationToken ct)
    {
        using var client = new HttpClient { BaseAddress = new Uri(endpoint.TrimEnd('/')) };

        console.MarkupLine($"[yellow]Removing plugin:[/] {pluginId}");

        try
        {
            var response = await client.PostAsync($"/admin/plugins/{pluginId}/unload", null, ct);

            if (response.IsSuccessStatusCode)
            {
                console.MarkupLine($"[green]Plugin '{pluginId}' removed successfully![/]");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                console.MarkupLine($"[red]Failed to remove plugin:[/] {error}");
                return 1;
            }
        }
        catch (Exception ex)
        {
            console.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }

        return 0;
    }

    private static string MapType(string type) => type.ToLowerInvariant() switch
    {
        "dotnet-assembly" or "dotnetassembly" => "DotNetAssembly",
        "openapi" or "openapispec" => "OpenApiSpec",
        "mcp" or "mcpserver" => "McpServer",
        "script" => "Script",
        "executable" => "Executable",
        _ => "DotNetAssembly"
    };
}
