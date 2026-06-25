using System.CommandLine;
using System.Net.Http.Json;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class ReviewCommand
{
    public Command Build()
    {
        var endpointOpt = new Option<string>("--endpoint")
        {
            Description = "Backend endpoint URL",
            DefaultValueFactory = _ => "http://localhost:5235"
        };
        var outputOpt = new Option<string>("--output")
        {
            Description = "Output file path (writes result to file)"
        };
        var pathOpt = new Option<DirectoryInfo>("--path")
        {
            Description = "Repository path (defaults to current directory)"
        };

        var cmd = new Command("review", "Review current uncommitted git diff")
        {
            endpointOpt, outputOpt, pathOpt
        };

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var endpoint = r.GetValue(endpointOpt) ?? "http://localhost:5235";
            var output = r.GetValue(outputOpt);
            var path = r.GetValue(pathOpt);

            var repoPath = path?.FullName ?? Directory.GetCurrentDirectory();

            AnsiConsole.MarkupLine($"[grey]Getting git diff in {repoPath}...[/]");

            var diff = await GetGitDiffAsync(repoPath).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(diff))
            {
                AnsiConsole.MarkupLine("[yellow]No uncommitted changes found.[/]");
                return 0;
            }

            AnsiConsole.MarkupLine($"[grey]Diff size: {diff.Length} chars[/]");

            using var http = new HttpClient
            {
                BaseAddress = new Uri(endpoint),
                Timeout = TimeSpan.FromSeconds(120)
            };

            try
            {
                AnsiConsole.MarkupLine("[grey]Sending diff for review...[/]");

                var response = await http.PostAsJsonAsync("/agent/review-diff",
                    new { diff, path = repoPath, mode = "gateway" }, ct).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ReviewResult>(ct).ConfigureAwait(false);
                    var text = result?.Review ?? result?.Error ?? "Sem resposta";

                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        await File.WriteAllTextAsync(output, text, ct).ConfigureAwait(false);
                        AnsiConsole.MarkupLine($"[green]Review written to: {output}[/]");
                    }
                    else
                    {
                        Console.WriteLine(text);
                    }

                    return 0;
                }

                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
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
                Console.Error.WriteLine("Timeout after 120s");
                return 1;
            }
        });

        return cmd;
    }

    private static async Task<string> GetGitDiffAsync(string repoPath)
    {
        try
        {
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "diff HEAD",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = repoPath
                }
            };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            await process.WaitForExitAsync().ConfigureAwait(false);

            if (process.ExitCode != 0)
                return string.Empty;

            if (string.IsNullOrWhiteSpace(output))
            {
                using var staged = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "diff --cached",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = repoPath
                    }
                };
                staged.Start();
                output = await staged.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                await staged.WaitForExitAsync().ConfigureAwait(false);
            }

            return output ?? string.Empty;
        }
        catch
        {
            AnsiConsole.MarkupLine("[red]Failed to run git. Ensure git is installed and in PATH.[/]");
            return string.Empty;
        }
    }

    private sealed record ReviewResult(string? Review, string? Error);
}
