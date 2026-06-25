using System.CommandLine;
using System.Net.Http.Json;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class ReviewPrCommand
{
    public Command Build()
    {
        var prArg = new Argument<int>("pr-number")
        {
            Description = "Pull Request number to review"
        };
        var endpointOpt = new Option<string>("--endpoint")
        {
            Description = "Backend endpoint URL",
            DefaultValueFactory = _ => "http://localhost:5235"
        };
        var outputOpt = new Option<string>("--output")
        {
            Description = "Output file path (writes result to file)"
        };
        var repoOpt = new Option<string>("--repo")
        {
            Description = "Repository in format owner/name (uses git remote if not specified)"
        };

        var cmd = new Command("review-pr", "Review a GitHub Pull Request")
        {
            prArg, endpointOpt, outputOpt, repoOpt
        };

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var prNumber = r.GetValue(prArg);
            var endpoint = r.GetValue(endpointOpt) ?? "http://localhost:5235";
            var output = r.GetValue(outputOpt);
            var repo = r.GetValue(repoOpt);

            repo ??= await DetectRepoAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(repo))
            {
                AnsiConsole.MarkupLine("[red]Could not detect repository. Specify --repo owner/name[/]");
                return 1;
            }

            AnsiConsole.MarkupLine($"[grey]Reviewing PR #{prNumber} from {repo}...[/]");

            using var http = new HttpClient
            {
                BaseAddress = new Uri(endpoint),
                Timeout = TimeSpan.FromSeconds(120)
            };

            try
            {
                var response = await http.PostAsJsonAsync("/agent/review-pr",
                    new { prNumber, repo, mode = "gateway" }, ct).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ReviewPrResult>(ct).ConfigureAwait(false);
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

    private static async Task<string?> DetectRepoAsync()
    {
        try
        {
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "remote get-url origin",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var url = (await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false)).Trim();
            await process.WaitForExitAsync().ConfigureAwait(false);

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(url))
                return null;

            return url switch
            {
                { } u when u.Contains("github.com/") => ExtractRepoFromUrl(u),
                { } u when u.StartsWith("git@") => ExtractRepoFromSshUrl(u),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractRepoFromUrl(string url)
    {
        var parts = url.Replace("https://github.com/", "")
                       .Replace(".git", "")
                       .Split('/');
        return parts.Length >= 2 ? $"{parts[0]}/{parts[1]}" : null;
    }

    private static string? ExtractRepoFromSshUrl(string url)
    {
        var parts = url.Replace("git@github.com:", "")
                       .Replace(".git", "")
                       .Split('/');
        return parts.Length >= 2 ? $"{parts[0]}/{parts[1]}" : null;
    }

    private sealed record ReviewPrResult(string? Review, string? Error);
}
