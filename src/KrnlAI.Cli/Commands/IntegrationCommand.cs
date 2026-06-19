using System.CommandLine;
using System.Net.Http.Json;
using System.Text.Json;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class IntegrationCommand(IAnsiConsole console)
{
    public Command Build()
    {
        var cmd = new Command("integration", "Manage provider integrations")
        {
            BuildList(),
            BuildTest(),
            BuildConfig(),
            BuildAdd()
        };

        return cmd;
    }

    private Command BuildList()
    {
        var endpointOpt = new Option<string>("--endpoint")
        {
            Description = "LLM Gateway endpoint",
            DefaultValueFactory = _ => "http://localhost:5001"
        };
        var cmd = new Command("list", "List available integrations") { endpointOpt };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var endpoint = r.GetValue(endpointOpt)!;
            try
            {
                using var client = new HttpClient { BaseAddress = new Uri(endpoint.TrimEnd('/')) };
                var response = await client.GetAsync("/api/integrations/providers", ct);
                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadFromJsonAsync<JsonDocument>(ct);
                if (body is null)
                {
                    console.MarkupLine("[yellow]No response from endpoint[/]");
                    return 1;
                }

                if (!body.RootElement.TryGetProperty("providers", out var providers) || providers.ValueKind != JsonValueKind.Array)
                {
                    console.MarkupLine("[yellow]No integrations found[/]");
                    return 0;
                }

                var table = new Table();
                table.AddColumn("Name");
                table.AddColumn("Version");
                table.AddColumn("Streaming");
                table.AddColumn("Vision");
                table.AddColumn("Tools");
                table.AddColumn("Models");

                foreach (var p in providers.EnumerateArray())
                {
                    var caps = p.GetProperty("capabilities");
                    var models = string.Join(", ",
                        caps.GetProperty("models").EnumerateArray().Select(m => m.GetString()));
                    table.AddRow(
                        p.GetProperty("name").GetString() ?? "",
                        p.GetProperty("version").GetString() ?? "",
                        caps.GetProperty("supportsStreaming").GetBoolean() ? "[green]Yes[/]" : "[red]No[/]",
                        caps.GetProperty("supportsVision").GetBoolean() ? "[green]Yes[/]" : "[red]No[/]",
                        caps.GetProperty("supportsToolUse").GetBoolean() ? "[green]Yes[/]" : "[red]No[/]",
                        models
                    );
                }

                console.Write(table);
                return 0;
            }
            catch (Exception ex)
            {
                console.MarkupLineInterpolated($"[red]Error: {ex.Message}[/]");
                return 1;
            }
        });
        return cmd;
    }

    private Command BuildTest()
    {
        var nameArg = new Argument<string>("name") { Description = "Provider name (e.g., OpenAI, Anthropic)" };
        var endpointOpt = new Option<string>("--endpoint")
        {
            Description = "LLM Gateway endpoint",
            DefaultValueFactory = _ => "http://localhost:5001"
        };
        var cmd = new Command("test", "Test provider connection") { nameArg, endpointOpt };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var name = r.GetValue(nameArg)!;
            var endpoint = r.GetValue(endpointOpt)!;

            try
            {
                using var client = new HttpClient { BaseAddress = new Uri(endpoint.TrimEnd('/')) };
                var response = await client.GetAsync($"/api/integrations/health/{name}", ct);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<JsonDocument>(ct);
                if (result is null)
                {
                    console.MarkupLine("[red]No response[/]");
                    return 1;
                }

                var status = result.RootElement.GetProperty("status").GetString() ?? "unknown";
                var latency = result.RootElement.TryGetProperty("latencyMs", out var lat)
                    ? $"{lat.GetDouble():F0}ms" : "N/A";

                var color = status switch
                {
                    "healthy" => "green",
                    "degraded" => "yellow",
                    _ => "red"
                };

                console.MarkupLineInterpolated($"[{color}]{name}: {status} ({latency})[/]");
                return status == "healthy" ? 0 : 1;
            }
            catch (Exception ex)
            {
                console.MarkupLineInterpolated($"[red]Error testing {name}: {ex.Message}[/]");
                return 1;
            }
        });
        return cmd;
    }

    private Command BuildConfig()
    {
        var nameArg = new Argument<string>("name") { Description = "Provider name" };
        var cmd = new Command("config", "Show provider configuration") { nameArg };
        cmd.SetAction((ParseResult r, CancellationToken ct) =>
        {
            var name = r.GetValue(nameArg)!;
            var configKeys = name switch
            {
                "OpenAI" => new[] { "OPENAI_API_KEY", "OPENAI_MODEL" },
                "Anthropic" => ["ANTHROPIC_API_KEY", "ANTHROPIC_MODEL"],
                "AzureOpenAI" => ["AZURE_API_KEY", "AZURE_ENDPOINT", "AZURE_DEPLOYMENT"],
                "AWSBedrock" => ["BEDROCK_ACCESS_KEY", "BEDROCK_SECRET_KEY", "BEDROCK_REGION"],
                "Groq" => ["GROQ_API_KEY", "GROQ_MODEL"],
                "TogetherAI" => ["TOGETHER_API_KEY", "TOGETHER_MODEL"],
                "Cohere" => ["COHERE_API_KEY", "COHERE_MODEL"],
                "DeepSeek" => ["DEEPSEEK_API_KEY", "DEEPSEEK_MODEL"],
                "Gemini" => ["GEMINI_API_KEY", "GEMINI_MODEL"],
                "Mistral" => ["MISTRAL_API_KEY", "MISTRAL_MODEL"],
                "Ollama" => ["OLLAMA_BASE_URL", "OLLAMA_MODEL"],
                "OpenRouter" => ["OPENROUTER_API_KEY", "OPENROUTER_MODEL"],
                _ => [$"{name.ToUpperInvariant()}_API_KEY"]
            };

            var table = new Table();
            table.AddColumn("Variable");
            table.AddColumn("Status");
            table.AddColumn("Value");

            foreach (var key in configKeys)
            {
                var val = Environment.GetEnvironmentVariable(key);
                var masked = val is not null
                    ? (val.Length > 8 ? val[..4] + "****" : "****")
                    : "";
                table.AddRow(
                    key,
                    val is not null ? "[green]Set[/]" : "[red]Not set[/]",
                    masked
                );
            }

            console.MarkupLineInterpolated($"[bold]Configuration for {name}[/]");
            console.Write(table);
            return Task.FromResult(0);
        });
        return cmd;
    }

    private Command BuildAdd()
    {
        var nameArg = new Argument<string>("name") { Description = "Provider name" };
        var cmd = new Command("add", "Add provider configuration to .env") { nameArg };
        cmd.SetAction((ParseResult r, CancellationToken ct) =>
        {
            var name = r.GetValue(nameArg)!;
            var envFile = Path.Combine(Environment.CurrentDirectory, ".env");
            var lines = new List<string>();

            if (File.Exists(envFile))
                lines.AddRange(File.ReadAllLines(envFile));

            var newVars = name switch
            {
                "OpenAI" => new[] { "OPENAI_API_KEY=sk-...", "OPENAI_MODEL=gpt-4o" },
                "Anthropic" => ["ANTHROPIC_API_KEY=sk-ant-...", "ANTHROPIC_MODEL=claude-sonnet-4-20250514"],
                "AzureOpenAI" => ["AZURE_API_KEY=...", "AZURE_ENDPOINT=https://...", "AZURE_DEPLOYMENT=gpt-4o"],
                "Groq" => ["GROQ_API_KEY=gsk_...", "GROQ_MODEL=llama3-70b-8192"],
                "TogetherAI" => ["TOGETHER_API_KEY=...", "TOGETHER_MODEL=mistralai/Mixtral-8x22B-Instruct-v0.1"],
                "Cohere" => ["COHERE_API_KEY=...", "COHERE_MODEL=command-r-plus"],
                "DeepSeek" => ["DEEPSEEK_API_KEY=sk-...", "DEEPSEEK_MODEL=deepseek-chat"],
                "Gemini" => ["GEMINI_API_KEY=...", "GEMINI_MODEL=gemini-2.5-pro"],
                "Mistral" => ["MISTRAL_API_KEY=...", "MISTRAL_MODEL=mistral-large-latest"],
                "Ollama" => ["OLLAMA_BASE_URL=http://localhost:11434", "OLLAMA_MODEL=llama3"],
                "OpenRouter" => ["OPENROUTER_API_KEY=sk-or-...", "OPENROUTER_MODEL=openai/gpt-4.1-mini"],
                _ => [$"{name.ToUpperInvariant()}_API_KEY=..."]
            };

            var added = 0;
            foreach (var nv in newVars)
            {
                var key = nv.Split('=')[0];
                if (!lines.Any(l => l.StartsWith(key + "=")))
                {
                    lines.Add(nv);
                    added++;
                }
            }

            File.WriteAllLines(envFile, lines);
            console.MarkupLineInterpolated($"[green]Added {added} config variable(s) to .env for {name}[/]");
            return Task.FromResult(0);
        });
        return cmd;
    }
}
