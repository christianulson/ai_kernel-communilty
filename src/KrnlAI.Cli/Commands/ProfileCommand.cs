using System.CommandLine;
using System.Text.Json;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class ProfileCommand
{
    public Command Build()
    {
        var showCmd = new Command("show", "Show user profile");
        showCmd.SetAction(async (_, ct) =>
        {
            var baseUrl = Environment.GetEnvironmentVariable("KRNL__API_BASE_URL") ?? "http://localhost:5235";
            var userId = Environment.GetEnvironmentVariable("KRNL_USER_ID") ?? "dev-user";
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var response = await http.GetAsync($"{baseUrl}/profile/{userId}", ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    AnsiConsole.MarkupLine("[red]Failed to fetch profile[/]");
                    return;
                }
                var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var table = new Table().Border(TableBorder.Rounded);
                table.AddColumn("Property"); table.AddColumn("Value");
                foreach (var prop in root.EnumerateObject())
                    table.AddRow(prop.Name, prop.Value.ToString() ?? "");
                AnsiConsole.Write(table);
            }
            catch (Exception ex) { AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]"); }
        });

        var cmd = new Command("profile", "View user profile") { showCmd };
        return cmd;
    }
}
