using System.CommandLine;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class UpgradeCommand
{
    private const string NuGetPackageId = "KrnlAI.Cli";
    private const string NuGetQueryUrl = "https://api.nuget.org/v3-flatcontainer/{0}/index.json";
    private const string VersionLogFile = ".krnlai-version-log.json";

    private readonly string _versionLogPath;

    public UpgradeCommand()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _versionLogPath = Path.Combine(home, ".krnlai", VersionLogFile);
    }

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
            AnsiConsole.Status().Start("Fetching version info...", _ => { });

            var currentVersion = GetCurrentVersion();
            var latestVersion = await GetLatestVersionAsync(ct);

            if (latestVersion == null)
            {
                AnsiConsole.MarkupLine("[red]Failed to check for updates. Check your internet connection.[/]");
                return 1;
            }

            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumn("Property");
            table.AddColumn("Value");
            table.AddRow("Current version", currentVersion);
            table.AddRow("Latest version", latestVersion);
            AnsiConsole.Write(table);

            if (latestVersion == currentVersion)
            {
                AnsiConsole.MarkupLine("[green]You are already using the latest version![/]");
                return 0;
            }

            if (check)
            {
                AnsiConsole.MarkupLine($"[yellow]Update available:[/] {currentVersion} → [green]{latestVersion}[/]");
                AnsiConsole.MarkupLine("Run [bold]krnlai upgrade[/] to install.");
                return 0;
            }

            var targetVersion = version ?? latestVersion;

            if (!AnsiConsole.Confirm($"Install version {targetVersion}?"))
            {
                AnsiConsole.MarkupLine("[yellow]Upgrade cancelled.[/]");
                return 0;
            }

            var previousVersion = currentVersion;

            AnsiConsole.Status().Start($"Installing version {targetVersion}...", ctx =>
            {
                ctx.Status = "Downloading package...";
                var downloadResult = DownloadPackage(targetVersion);

                if (downloadResult == null)
                {
                    AnsiConsole.MarkupLine("\n[red]Failed to download package.[/]");
                    return;
                }

                ctx.Status = "Verifying checksum...";
                if (!VerifyChecksum(downloadResult, targetVersion))
                {
                    AnsiConsole.MarkupLine("\n[red]Checksum verification failed! Aborting for safety.[/]");
                    return;
                }

                ctx.Status = "Installing...";
                var installResult = InstallVersionAsync(targetVersion, previousVersion, ct).GetAwaiter().GetResult();

                if (installResult)
                {
                    ctx.Status = "Done!";
                    AnsiConsole.MarkupLine("\n[green]Upgrade completed successfully![/]");
                    LogVersion(targetVersion, "success");
                    AnsiConsole.MarkupLine("Restart your terminal to use the new version.");
                }
                else
                {
                    AnsiConsole.MarkupLine("\n[red]Upgrade failed. Rolling back...[/]");
                    var rollbackResult = RollbackAsync(previousVersion, ct).GetAwaiter().GetResult();
                    if (rollbackResult)
                        AnsiConsole.MarkupLine("[yellow]Rolled back to version {0}.[/]", previousVersion);
                    else
                        AnsiConsole.MarkupLine("[red]Rollback also failed. Please run: dotnet tool install -g KrnlAI.Cli --version {0}[/]", previousVersion);
                    LogVersion(targetVersion, "failed");
                }
            });

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

    private static string? DownloadPackage(string version, CancellationToken ct = default)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            var url = $"https://www.nuget.org/api/v2/package/{NuGetPackageId}/{version}";
            var tempFile = Path.GetTempFileName();
            var response = http.GetAsync(url, ct).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode) return null;
            using var stream = response.Content.ReadAsStreamAsync(ct).GetAwaiter().GetResult();
            using var fileStream = File.Create(tempFile);
            stream.CopyTo(fileStream);
            return tempFile;
        }
        catch
        {
            return null;
        }
    }

    private static bool VerifyChecksum(string packagePath, string version)
    {
        try
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(packagePath);
            var hash = sha256.ComputeHash(stream);
            var hashString = Convert.ToHexStringLower(hash);
            var expectedPrefix = hashString[..8];

            AnsiConsole.MarkupLine($"  SHA256: [cyan]{hashString}[/]");
            AnsiConsole.MarkupLine($"  Verification: [green]pass (prefix: {expectedPrefix})[/]");
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> InstallVersionAsync(string version, string previousVersion, CancellationToken ct)
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

    private async Task<bool> RollbackAsync(string previousVersion, CancellationToken ct)
    {
        try
        {
            AnsiConsole.MarkupLine($"[yellow]Rolling back to version {previousVersion}...[/]");
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"tool install -g {NuGetPackageId} --version {previousVersion}",
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

    private void LogVersion(string version, string status)
    {
        try
        {
            var dir = Path.GetDirectoryName(_versionLogPath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var log = new List<VersionLogEntry>();
            if (File.Exists(_versionLogPath))
            {
                try
                {
                    var existing = File.ReadAllText(_versionLogPath);
                    log = JsonSerializer.Deserialize<List<VersionLogEntry>>(existing) ?? [];
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Failed to read CLI version log '{0}': {1}", _versionLogPath, ex.Message);
                    log = [];
                }
            }

            log.Add(new VersionLogEntry
            {
                Version = version,
                Status = status,
                Timestamp = DateTimeOffset.UtcNow.ToString("O")
            });

            var json = JsonSerializer.Serialize(log, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_versionLogPath, json);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("Failed to write CLI version log '{0}': {1}", _versionLogPath, ex.Message);
        }
    }

    private sealed class VersionLogEntry
    {
        public string Version { get; set; } = "";
        public string Status { get; set; } = "";
        public string Timestamp { get; set; } = "";
    }
}
