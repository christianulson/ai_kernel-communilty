using System.CommandLine;
using System.Net.Http.Json;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class RunCommand
{
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

        var cmd = new Command("run", "Execute a single prompt (non-interactive / pipe mode)")
        {
            promptArg, endpointOpt, timeoutOpt, outputOpt
        };

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var prompts = r.GetValue(promptArg);
            var endpoint = r.GetValue(endpointOpt) ?? "http://localhost:5000";
            var timeout = r.GetValue(timeoutOpt);
            var output = r.GetValue(outputOpt);

            string input;

            if (prompts is { Length: > 0 })
            {
                input = string.Join(" ", prompts);
                AnsiConsole.MarkupLine($"[grey]Prompt:[/] {input.EscapeMarkup()}");
            }
            else
            {
                if (Console.IsInputRedirected)
                {
                    using var reader = new StreamReader(Console.OpenStandardInput());
                    input = await reader.ReadToEndAsync(ct);
                    AnsiConsole.MarkupLine($"[grey]Read {input.Length} chars from stdin[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]No input provided. Usage:[/]");
                    AnsiConsole.MarkupLine("  [cyan]aikernel run \"your prompt\"[/]");
                    AnsiConsole.MarkupLine("  [cyan]cat file.cs | aikernel run[/]");
                    return 1;
                }
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
                AnsiConsole.MarkupLine("[grey]Sending to backend...[/]");

                var response = await http.PostAsJsonAsync("/agent/run",
                    new { prompt = input.Trim(), mode = "gateway" }, ct);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<RunResult>(ct);
                    var text = result?.Narration ?? result?.Error ?? "Sem resposta";

                    if (!string.IsNullOrWhiteSpace(output))
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

                var error = await response.Content.ReadAsStringAsync(ct);
                Console.Error.WriteLine($"Error {response.StatusCode}: {error}");
                return 1;
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"Connection error: {ex.Message}");
                AnsiConsole.MarkupLine($"[yellow]Tip: Ensure the backend is running at {endpoint}[/]");
                return 1;
            }
            catch (TaskCanceledException)
            {
                Console.Error.WriteLine($"Timeout after {timeout}s");
                return 1;
            }
        });

        return cmd;
    }

    private sealed record RunResult(string? Narration, string? Error);
}
