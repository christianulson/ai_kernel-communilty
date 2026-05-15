using System.CommandLine;
using AIKernel.Cli.Services;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class ArchiveCommand(CliContext ctx, ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("archive", "Manage archives");

        var list = new Command("list", "List archived entries");
        var takeOpt = new Option<int>("--take")
        {
            Description = "Max entries", DefaultValueFactory = _ => 10
        };
        list.Add(takeOpt);
        list.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var take = r.GetValue(takeOpt);
            var entries = await ctx.ArchiveStore.ListRecentAsync(take, reason: null, ct);
            if (entries.Count == 0)
            {
                renderer.Console.MarkupLine("[yellow]No archived entries[/]");
                return 0;
            }
            var rows = entries.Select(e => new
            {
                e.OriginalId,
                Store = e.StoreName,
                e.Reason,
                Utility = e.UtilityAtDeath?.ToString("F2") ?? "N/A",
                Forgotten = e.ForgottenAt.ToString("yyyy-MM-dd HH:mm"),
                PurgesAt = e.PurgeAfter.ToString("yyyy-MM-dd")
            }).ToList();
            renderer.RenderTable(rows, "OriginalId", "Store", "Reason", "UtilityAtDeath", "Forgotten", "Purges");
            return 0;
        });
        cmd.Add(list);

        var count = new Command("count", "Count archived entries");
        count.SetAction(async (ParseResult _, CancellationToken ct) =>
        {
            var total = await ctx.ArchiveStore.CountArchivedAsync(ct);
            renderer.Console.MarkupLine($"[bold]Archived entries:[/] {total}");
            return 0;
        });
        cmd.Add(count);

        var purge = new Command("purge", "Purge expired entries");
        purge.SetAction(async (ParseResult _, CancellationToken ct) =>
        {
            var purged = await ctx.ArchiveStore.PurgeExpiredAsync(ct);
            renderer.Console.MarkupLine($"[green]Purged {purged} expired entr{(purged == 1 ? "y" : "ies")}[/]");
            return 0;
        });
        cmd.Add(purge);

        return cmd;
    }
}