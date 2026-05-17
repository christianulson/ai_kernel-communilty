using System.CommandLine;
using System.Net.Http.Json;
using System.Text.Json;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class RunCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public Command Build()
    {
        var promptArg = new Argument<string[]>("prompt")
        {
            Description = "Prompt to execute (reads from stdin if empty)",
            Arity = ArgumentArity.ZeroOrMore
        };
        var endpointOpt = new Option<string>("--endpoint")
        {
            Description = "Backend endpoint URL",
            DefaultValueFactory = _ => "http://localhost:5000"
        };
        var timeoutOpt = new Option<int>("--timeout")
        {
            Description = "Timeout in seconds",
            DefaultValueFactory = _ => 60
        };
        var outputOpt = new Option<string>("--output")
        {
            Description = "Output file path (writes result to file)"
        };
        var pipeOpt = new Option<bool>("--pipe")
        {
            Description = "Force pipe mode (read from stdin)"
        };
        var jsonOpt = new Option<bool>("--json")
        {
            Description = "Output as structured JSON"
        };

        var cmd = new Command("run", "Execute a single prompt (non-interactive / pipe mode)")
        {
            promptArg, endpointOpt, timeoutOpt, outputOpt, pipeOpt, jsonOpt
        };

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var prompts = r.GetValue(promptArg);
            var endpoint = r.GetValue(endpointOpt) ?? "http://localhost:5000";
            var timeout = r.GetValue(timeoutOpt);
            var output = r.GetValue(outputOpt);
            var pipe = r.GetValue(pipeOpt);
            var json = r.GetValue(jsonOpt);

            string input;

            if (prompts is { Length: > 0 } && !pipe)
            {
                input = string.Join(" ", prompts);
                if (!json)
                    AnsiConsole.MarkupLine($"[grey]Prompt:[/] {input.EscapeMarkup()}");
            }
            else if (pipe || Console.IsInputRedirected)
            {
                using var reader = new StreamReader(Console.OpenStandardInput());
                input = await reader.ReadToEndAsync(ct);
                if (!json)
                    AnsiConsole.MarkupLine($"[grey]Read {input.Length} chars from stdin[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]No input provided. Usage:[/]");
                AnsiConsole.MarkupLine("  [cyan]aikernel run \"your prompt\"[/]");
                AnsiConsole.MarkupLine("  [cyan]cat file.cs | aikernel run[/]");
                AnsiConsole.MarkupLine("  [cyan]cat data.json | aikernel run --json \"Validate\"[/]");
                return 1;
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                AnsiConsole.MarkupLine("[red]Empty input. Nothing to execute.[/]");
                return 1;
            }

            using var http = new HttpClient
            {
                BaseAddress = new Uri(endpoint),
                Timeout = TimeSpan.FromSeconds(timeout)
            };

            try
            {
                if (!json)
                    AnsiConsole.MarkupLine("[grey]Sending to backend...[/]");

                var response = await http.PostAsJsonAsync("/agent/run",
                    new { prompt = input.Trim(), mode = "gateway" }, ct);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<RunResult>(ct);
                    var text = result?.Narration ?? result?.Error ?? "Sem resposta";

                    if (json)
                    {
                        var jsonResult = JsonSerializer.Serialize(new
                        {
                            success = true,
                            result?.Narration,
                            result?.Error,
                            prompt = input.Trim()
                        }, JsonOptions);
                        Console.WriteLine(jsonResult);
                    }
                    else if (!string.IsNullOrWhiteSpace(output))
                    {
                        await File.WriteAllTextAsync(output, text, ct);
                        AnsiConsole.MarkupLine($"[green]Result written to: {output}[/]");
                    }
                    else
                    {
                        Console.WriteLine(text);
                    }

                    return 0;
                }

                var errorBody = await response.Content.ReadAsStringAsync(ct);

                if (json)
                {
                    var jsonError = JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = $"HTTP {response.StatusCode}: {errorBody}",
                        prompt = input.Trim()
                    }, JsonOptions);
                    Console.WriteLine(jsonError);
                }
                else
                {
                    Console.Error.WriteLine($"Error {response.StatusCode}: {errorBody}");
                }
                return 1;
            }
            catch (HttpRequestException ex)
            {
                if (json)
                {
                    var jsonError = JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = $"Connection error: {ex.Message}"
                    }, JsonOptions);
                    Console.WriteLine(jsonError);
                }
                else
                {
                    Console.Error.WriteLine($"Connection error: {ex.Message}");
                    AnsiConsole.MarkupLine($"[yellow]Tip: Ensure the backend is running at {endpoint}[/]");
                }
                return 1;
            }
            catch (TaskCanceledException)
            {
                if (json)
                {
                    var jsonError = JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = $"Timeout after {timeout}s"
                    }, JsonOptions);
                    Console.WriteLine(jsonError);
                }
                else
                {
                    Console.Error.WriteLine($"Timeout after {timeout}s");
                }
                return 1;
            }
        });

        return cmd;
    }

    private sealed record RunResult(string? Narration, string? Error);
}
