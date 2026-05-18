using System.CommandLine;
using System.Net.Http.Json;
using System.Text.Json;
using Kernel.Core.Abstractions;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class PluginCommand(IAnsiConsole console, IAssemblyPluginLoader? localLoader = null)
{
    private readonly List<string> _localPluginIds = new();

    public Command Build()
    {
        var cmd = new Command("plugin", "Manage plugins");

        cmd.Add(BuildInstall());
        cmd.Add(BuildList());
        cmd.Add(BuildRemove());

        return cmd;
    }

    private Command BuildInstall()
    {
        var pathArg = new Argument<string>("path") { Description = "Plugin path or spec URL" };
        var endpointOpt = new Option<string>("--endpoint")
        {
            Description = "Kernel API endpoint (remote mode)",
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

            if (local)
                return await InstallLocalAsync(path, ct);

            var endpoint = r.GetValue(endpointOpt)!;
            var type = r.GetValue(typeOpt)!;
            return await InstallRemoteAsync(path, endpoint, type, ct);
        });

        return cmd;
    }

    private Command BuildList()
    {
        var endpointOpt = new Option<string>("--endpoint")
        {
            Description = "Kernel API endpoint (remote mode)",
            DefaultValueFactory = _ => "http://localhost:5000"
        };
        var localOpt = new Option<bool>("--local")
        {
            Description = "List locally loaded plugins"
        };

        var cmd = new Command("list", "List installed plugins") { endpointOpt, localOpt };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            if (r.GetValue(localOpt))
            {
                return ListLocal();
            }

            var endpoint = r.GetValue(endpointOpt)!;
            return await ListRemoteAsync(endpoint, ct);
        });

        return cmd;
    }

    private Command BuildRemove()
    {
        var pluginIdArg = new Argument<string>("plugin-id") { Description = "Plugin ID to remove" };
        var endpointOpt = new Option<string>("--endpoint")
        {
            Description = "Kernel API endpoint (remote mode)",
            DefaultValueFactory = _ => "http://localhost:5000"
        };
        var localOpt = new Option<bool>("--local")
        {
            Description = "Remove locally loaded plugin"
        };

        var cmd = new Command("remove", "Remove a plugin") { pluginIdArg, endpointOpt, localOpt };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var pluginId = r.GetValue(pluginIdArg)!;
            var local = r.GetValue(localOpt);

            if (local)
            {
                return RemoveLocal(pluginId);
            }

            var endpoint = r.GetValue(endpointOpt)!;
            return await RemoveRemoteAsync(pluginId, endpoint, console, ct);
        });

        return cmd;
    }

    private async Task<int> InstallLocalAsync(string path, CancellationToken ct)
    {
        if (localLoader is null)
        {
            console.MarkupLine("[red]Local plugin loader not available. Use --endpoint for remote mode.[/]");
            return 1;
        }

        if (!File.Exists(path))
        {
            console.MarkupLine($"[red]File not found:[/] {path}");
            return 1;
        }

        console.MarkupLine($"[blue]Loading plugin locally:[/] {path}");

        try
        {
            var manifest = new PluginManifest(
                Id: Path.GetFileNameWithoutExtension(path).ToLowerInvariant(),
                Name: Path.GetFileNameWithoutExtension(path),
                Version: "1.0.0",
                Description: $"Plugin from {path}",
                Author: "CLI",
                Permissions: new[] { "tools.execute" });

            var plugin = await localLoader.LoadFromAssemblyAsync(path, manifest, ct);
            _localPluginIds.Add(manifest.Id);
            console.MarkupLine($"[green]Plugin '{manifest.Id}' loaded from:[/] {path}");
            return 0;
        }
        catch (Exception ex)
        {
            console.MarkupLine($"[red]Error loading plugin:[/] {ex.Message}");
            return 1;
        }
    }

    private int ListLocal()
    {
        if (_localPluginIds.Count == 0)
        {
            console.MarkupLine("[yellow]No locally loaded plugins[/]");
            return 0;
        }

        var table = new Table();
        table.AddColumn("Id");
        table.AddColumn("Path");
        foreach (var id in _localPluginIds)
            table.AddRow(id, "local");
        console.Write(table);
        return 0;
    }

    private int RemoveLocal(string pluginId)
    {
        if (!_localPluginIds.Remove(pluginId))
        {
            console.MarkupLine($"[red]Local plugin '{pluginId}' not found[/]");
            return 1;
        }

        localLoader?.Unload(pluginId);
        console.MarkupLine($"[green]Local plugin '{pluginId}' unloaded[/]");
        return 0;
    }

    private async Task<int> InstallRemoteAsync(string path, string endpoint, string type, CancellationToken ct)
    {
        using var client = new HttpClient { BaseAddress = new Uri(endpoint.TrimEnd('/')) };

        console.MarkupLine($"[blue]Installing plugin from:[/] {path}");
        console.MarkupLine($"[blue]Type:[/] {type}");

        try
        {
            var response = await client.PostAsJsonAsync("/admin/plugins/load", new
            {
                manifest = new
                {
                    id = Path.GetFileNameWithoutExtension(path).ToLowerInvariant(),
                    name = Path.GetFileNameWithoutExtension(path),
                    version = "1.0.0",
                    description = $"Plugin from {path}",
                    author = "CLI",
                    permissions = new[] { "tools.execute" },
                    pluginType = MapType(type)
                },
                permissions = new
                {
                    permissions = new[] { new { action = "tools.execute", resource = "tools.execute", isGranted = true } },
                    isolateFileSystem = true,
                    maxMemoryBytes = 268435456,
                    maxExecutionTime = "00:00:30",
                    isolationLevel = "None"
                }
            }, ct);

            var content = await response.Content.ReadAsStringAsync(ct);
            if (response.IsSuccessStatusCode)
            {
                console.MarkupLine($"[green]Plugin installed successfully![/]");
                console.MarkupLine($"[dim]{content}[/]");
            }
            else
            {
                console.MarkupLine($"[red]Failed to install plugin:[/] {content}");
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

    private static async Task<int> ListRemoteAsync(string endpoint, CancellationToken ct)
    {
        using var client = new HttpClient { BaseAddress = new Uri(endpoint.TrimEnd('/')) };

        try
        {
            var response = await client.GetAsync("/admin/plugins", ct);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<JsonDocument>(ct);
            if (body is null)
            {
                AnsiConsole.MarkupLine("[yellow]No response from endpoint[/]");
                return 1;
            }

            if (!body.RootElement.TryGetProperty("plugins", out var plugins) || plugins.ValueKind != JsonValueKind.Array)
            {
                AnsiConsole.MarkupLine("[yellow]No plugins found[/]");
                return 0;
            }

            var table = new Table();
            table.AddColumn("Id");
            table.AddColumn("Name");
            table.AddColumn("Version");
            table.AddColumn("State");

            foreach (var plugin in plugins.EnumerateArray())
            {
                table.AddRow(
                    plugin.GetProperty("id").GetString() ?? "",
                    plugin.GetProperty("name").GetString() ?? "",
                    plugin.GetProperty("version").GetString() ?? "",
                    plugin.GetProperty("state").GetString() ?? ""
                );
            }

            AnsiConsole.Write(table);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
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
