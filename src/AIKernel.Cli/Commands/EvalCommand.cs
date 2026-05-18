using System.CommandLine;
using System.Net.Http.Json;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class EvalCommand
{
    public Command Build()
    {
        var fileArg = new Argument<FileInfo>("file")
        {
            Description = "File to evaluate"
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

        var cmd = new Command("eval", "Evaluate a file (reads file and sends to backend)")
        {
            fileArg, endpointOpt, timeoutOpt, outputOpt
        };

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var file = r.GetValue(fileArg);
            var endpoint = r.GetValue(endpointOpt) ?? "http://localhost:5000";
            var timeout = r.GetValue(timeoutOpt);
            var output = r.GetValue(outputOpt);

            if (file is null || !file.Exists)
            {
                AnsiConsole.MarkupLine("[red]File not found.[/]");
                return 1;
            }

            var content = await File.ReadAllTextAsync(file.FullName, ct);

            if (string.IsNullOrWhiteSpace(content))
            {
                AnsiConsole.MarkupLine("[red]File is empty.[/]");
                return 1;
            }

            AnsiConsole.MarkupLine($"[grey]Reading: {file.FullName}[/]");
            AnsiConsole.MarkupLine($"[grey]Size: {content.Length} chars[/]");

            using var http = new HttpClient
            {
                BaseAddress = new Uri(endpoint),
                Timeout = TimeSpan.FromSeconds(timeout)
            };

            try
            {
                AnsiConsole.MarkupLine("[grey]Sending to backend for evaluation...[/]");

                var response = await http.PostAsJsonAsync("/agent/eval",
                    new { file = file.FullName, content, mode = "gateway" }, ct);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<EvalResult>(ct);
                    var text = result?.Evaluation ?? result?.Error ?? "Sem resposta";

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

    private sealed record EvalResult(string? Evaluation, string? Error);
}
