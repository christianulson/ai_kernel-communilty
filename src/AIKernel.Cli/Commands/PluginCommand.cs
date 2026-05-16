using System.CommandLine;
using System.CommandLine.Parsing;
using System.Net.Http.Json;
using System.Text.Json;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class PluginCommand(IAnsiConsole console)
{
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
            Description = "Kernel API endpoint",
            DefaultValueFactory = _ => "http://localhost:5000"
        };
        var typeOpt = new Option<string>("--type")
        {
            Description = "Plugin type: dotnet-assembly, openapi, mcp, script, executable",
            DefaultValueFactory = _ => "dotnet-assembly"
        };

        var cmd = new Command("install", "Install a plugin") { pathArg, endpointOpt, typeOpt };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var path = r.GetValue(pathArg)!;
            var endpoint = r.GetValue(endpointOpt)!;
            var type = r.GetValue(typeOpt)!;

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
        });

        return cmd;
    }

    private Command BuildList()
    {
        var endpointOpt = new Option<string>("--endpoint")
        {
            Description = "Kernel API endpoint",
            DefaultValueFactory = _ => "http://localhost:5000"
        };

        var cmd = new Command("list", "List installed plugins") { endpointOpt };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var endpoint = r.GetValue(endpointOpt)!;

            using var client = new HttpClient { BaseAddress = new Uri(endpoint.TrimEnd('/')) };

            try
            {
                var response = await client.GetAsync("/admin/plugins", ct);
                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadFromJsonAsync<JsonDocument>(ct);
                if (body is null)
                {
                    console.MarkupLine("[yellow]No response from endpoint[/]");
                    return 1;
                }

                if (!body.RootElement.TryGetProperty("plugins", out var plugins) || plugins.ValueKind != JsonValueKind.Array)
                {
                    console.MarkupLine("[yellow]No plugins found[/]");
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

                console.Write(table);
            }
            catch (Exception ex)
            {
                console.MarkupLine($"[red]Error:[/] {ex.Message}");
                return 1;
            }

            return 0;
        });

        return cmd;
    }

    private Command BuildRemove()
    {
        var pluginIdArg = new Argument<string>("plugin-id") { Description = "Plugin ID to remove" };
        var endpointOpt = new Option<string>("--endpoint")
        {
            Description = "Kernel API endpoint",
            DefaultValueFactory = _ => "http://localhost:5000"
        };

        var cmd = new Command("remove", "Remove a plugin") { pluginIdArg, endpointOpt };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var pluginId = r.GetValue(pluginIdArg)!;
            var endpoint = r.GetValue(endpointOpt)!;

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
        });

        return cmd;
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
