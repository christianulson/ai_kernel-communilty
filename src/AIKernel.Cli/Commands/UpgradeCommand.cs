using System.CommandLine;
using System.Diagnostics;
using System.Text.Json;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class UpgradeCommand
{
    private const string NuGetPackageId = "AIKernel.Cli";
    private const string NuGetQueryUrl = "https://api.nuget.org/v3-flatcontainer/{0}/index.json";

    public Command Build()
    {
        var checkOnly = new Option<bool>("--check") { Description = "Only check for updates, don't install" };
        var versionOpt = new Option<string>("--version") { Description = "Install specific version" };

        var cmd = new Command("upgrade", "Check for updates and upgrade AI Kernel CLI")
        {
            checkOnly, versionOpt
        };

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var check = r.GetValue(checkOnly);
            var version = r.GetValue(versionOpt);

            AnsiConsole.MarkupLine("[bold]Checking for updates...[/]");

            var currentVersion = GetCurrentVersion();
            AnsiConsole.MarkupLine($"Current version: [cyan]{currentVersion}[/]");

            var latestVersion = await GetLatestVersionAsync(ct);

            if (latestVersion == null)
            {
                AnsiConsole.MarkupLine("[red]Failed to check for updates. Check your internet connection.[/]");
                return 1;
            }

            AnsiConsole.MarkupLine($"Latest version: [cyan]{latestVersion}[/]");

            if (latestVersion == currentVersion)
            {
                AnsiConsole.MarkupLine("[green]You are already using the latest version![/]");
                return 0;
            }

            if (check)
            {
                AnsiConsole.MarkupLine($"[yellow]Update available: {currentVersion} → {latestVersion}[/]");
                AnsiConsole.MarkupLine("Run [bold]aikernel upgrade[/] to install.");
                return 0;
            }

            var targetVersion = version ?? latestVersion;

            if (!AnsiConsole.Confirm($"Install version {targetVersion}?"))
            {
                AnsiConsole.MarkupLine("[yellow]Upgrade cancelled.[/]");
                return 0;
            }

            AnsiConsole.MarkupLine($"[bold]Installing version {targetVersion}...[/]");

            var result = await InstallVersionAsync(targetVersion, ct);

            if (result)
            {
                AnsiConsole.MarkupLine("[green]Upgrade completed successfully![/]");
                AnsiConsole.MarkupLine("Restart your terminal to use the new version.");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Upgrade failed. Please try manually: dotnet tool update -g AIKernel.Cli[/]");
                return 1;
            }

            return 0;
        });

        return cmd;
    }

    private static string GetCurrentVersion()
    {
        var assembly = typeof(UpgradeCommand).Assembly;
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0";
    }

    private static async Task<string?> GetLatestVersionAsync(CancellationToken ct)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var url = string.Format(NuGetQueryUrl, NuGetPackageId.ToLowerInvariant());
            var response = await http.GetStringAsync(url, ct);

            using var doc = JsonDocument.Parse(response);
            var versions = doc.RootElement.GetProperty("versions");
            var latest = versions.EnumerateArray()
                .Select(v => v.GetString())
                .Where(v => v != null)
                .Select(v => v!.TrimStart('v'))
                .OrderBy(v => Version.TryParse(v, out var ver) ? ver : new Version(0, 0))
                .LastOrDefault();

            return latest;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<bool> InstallVersionAsync(string version, CancellationToken ct)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"tool update -g {NuGetPackageId} --version {version}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            await process.WaitForExitAsync(ct);

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
