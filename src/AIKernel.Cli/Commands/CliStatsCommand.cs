using System.CommandLine;
using AIKernel.Cli.Tui;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class CliStatsCommand
{
    public Command Build()
    {
        var cmd = new Command("stats", "Show usage statistics");

        var verboseOpt = new Option<bool>("--verbose") { Description = "Show detailed statistics" };
        cmd.Add(verboseOpt);

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var verbose = r.GetValue(verboseOpt);

            var store = new TuiSessionStore();
            var sessions = await store.ListAsync();

            var totalMessages = sessions.Sum(s => s.MessageCount);
            var totalSessions = sessions.Count;
            var totalTokens = EstimateTokens(sessions);
            var totalCost = totalTokens * 0.000002m;

            var firstDate = sessions.Count > 0
                ? sessions.Min(s => s.CreatedAt)
                : DateTimeOffset.MinValue;
            var lastDate = sessions.Count > 0
                ? sessions.Max(s => s.CreatedAt)
                : DateTimeOffset.MinValue;
            var activeDays = sessions
                .Select(s => s.CreatedAt.Date)
                .Distinct()
                .Count();

            var byDate = sessions
                .GroupBy(s => s.CreatedAt.Date)
                .OrderByDescending(g => g.Key)
                .Take(10)
                .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count());

            AnsiConsole.Write(new Rule("[bold]AI Kernel Usage Statistics[/]") { Style = Style.Parse("cyan") });

            var grid = new Grid()
                .AddColumn(new GridColumn().NoWrap().PadRight(2))
                .AddColumn();
            grid.AddRow("Total sessions:", totalSessions.ToString());
            grid.AddRow("Total messages:", totalMessages.ToString());
            grid.AddRow("Active days:", activeDays.ToString());
            grid.AddRow("Est. tokens:", totalTokens.ToString("N0"));
            grid.AddRow("Est. cost:", $"${totalCost:F4}");

            if (firstDate != DateTimeOffset.MinValue)
            {
                grid.AddRow("First activity:", firstDate.LocalDateTime.ToString("g"));
                grid.AddRow("Last activity:", lastDate.LocalDateTime.ToString("g"));
            }

            AnsiConsole.Write(new Panel(grid).Border(BoxBorder.Rounded));

            AnsiConsole.MarkupLine("\n[bold]Activity by Date:[/]");
            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumn("Date");
            table.AddColumn("Sessions");

            foreach (var (date, count) in byDate)
                table.AddRow(date, count.ToString());

            AnsiConsole.Write(table);

            if (verbose)
            {
                AnsiConsole.MarkupLine("\n[bold]All Sessions:[/]");
                var sessionTable = new Table().Border(TableBorder.Rounded);
                sessionTable.AddColumn("ID");
                sessionTable.AddColumn("Label");
                sessionTable.AddColumn("Messages");
                sessionTable.AddColumn("Date");

                foreach (var s in sessions.OrderByDescending(s => s.CreatedAt).Take(20))
                {
                    sessionTable.AddRow(
                        s.Id[..Math.Min(8, s.Id.Length)],
                        s.Label.Length > 30 ? s.Label[..30] + "..." : s.Label,
                        s.MessageCount.ToString(),
                        s.CreatedAt.LocalDateTime.ToString("g")
                    );
                }
                AnsiConsole.Write(sessionTable);
            }

            return 0;
        });

        return cmd;
    }

    private static int EstimateTokens(List<TuiSession> sessions)
    {
        int totalChars = 0;
        foreach (var session in sessions)
        {
            foreach (var msg in session.Messages)
            {
                totalChars += msg.Content.Length;
            }
        }
        return totalChars / 4;
    }
}
