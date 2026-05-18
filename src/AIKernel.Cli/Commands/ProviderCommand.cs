using System.CommandLine;
using System.Text.Json;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class ProviderCommand
{
    private readonly string _configPath;

    public ProviderCommand()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _configPath = Path.Combine(home, ".krnlai", "providers.json");
    }

    public Command Build()
    {
        var listCmd = new Command("list", "List configured LLM providers");
        listCmd.SetAction((ParseResult _, CancellationToken _) =>
        {
            var providers = LoadProviders();
            if (providers.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No providers configured.[/]");
                AnsiConsole.MarkupLine("Use [bold]krnlai provider add <name>[/] to add one.");
                return Task.FromResult(0);
            }

            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumn("Name");
            table.AddColumn("Model");
            table.AddColumn("Status");

            foreach (var (name, info) in providers)
            {
                var status = info.Enabled ? "[green]enabled[/]" : "[grey]disabled[/]";
                table.AddRow(name, info.DefaultModel ?? "-", status);
            }

            AnsiConsole.Write(table);
            return Task.FromResult(0);
        });

        var addCmd = new Command("add", "Add a new LLM provider")
        {
            new Argument<string>("name") { Description = "Provider name (openai, ollama, anthropic, etc.)" },
            new Option<string>("--api-key") { Description = "API key for the provider" },
            new Option<string>("--model") { Description = "Default model to use" },
            new Option<string>("--endpoint") { Description = "Custom endpoint URL" },
        };
        addCmd.SetAction((ParseResult r, CancellationToken _) =>
        {
            var name = r.GetValue<string>("name") ?? "";
            var apiKey = r.GetValue<string>("--api-key");
            var model = r.GetValue<string>("--model");
            var endpoint = r.GetValue<string>("--endpoint");

            var providers = LoadProviders();
            providers[name] = new ProviderInfo
            {
                DefaultModel = model,
                ApiKey = apiKey,
                Endpoint = endpoint,
                Enabled = true,
                AddedAt = DateTimeOffset.UtcNow.ToString("O"),
            };

            SaveProviders(providers);
            AnsiConsole.MarkupLine($"[green]Provider '{name}' added successfully.[/]");
            return Task.FromResult(0);
        });

        var removeCmd = new Command("remove", "Remove a provider")
        {
            new Argument<string>("name") { Description = "Provider name to remove" },
        };
        removeCmd.SetAction((ParseResult r, CancellationToken _) =>
        {
            var name = r.GetValue<string>("name") ?? "";
            var providers = LoadProviders();
            if (!providers.Remove(name))
            {
                AnsiConsole.MarkupLine($"[red]Provider '{name}' not found.[/]");
                return Task.FromResult(1);
            }

            SaveProviders(providers);
            AnsiConsole.MarkupLine($"[green]Provider '{name}' removed.[/]");
            return Task.FromResult(0);
        });

        var cmd = new Command("provider", "Manage LLM providers");
        cmd.Add(listCmd);
        cmd.Add(addCmd);
        cmd.Add(removeCmd);
        return cmd;
    }

    private Dictionary<string, ProviderInfo> LoadProviders()
    {
        try
        {
            if (!File.Exists(_configPath)) return [];
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<Dictionary<string, ProviderInfo>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void SaveProviders(Dictionary<string, ProviderInfo> providers)
    {
        var dir = Path.GetDirectoryName(_configPath)!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(providers, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }

    private sealed class ProviderInfo
    {
        public string? DefaultModel { get; set; }
        public string? ApiKey { get; set; }
        public string? Endpoint { get; set; }
        public bool Enabled { get; set; } = true;
        public string? AddedAt { get; set; }
    }
}
