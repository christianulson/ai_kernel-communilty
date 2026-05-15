using System.CommandLine;
using AIKernel.Cli.Services;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class MemoryCommand(CliContext ctx, ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("memory", "Search cognitive memory");

        var search = new Command("search", "Search memory by query");
        var queryArg = new Argument<string>("query") { Description = "Search query" };
        var takeOpt = new Option<int>("--take")
        {
            Description = "Max results", DefaultValueFactory = _ => 10
        };
        search.Add(queryArg);
        search.Add(takeOpt);
        search.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var query = r.GetValue(queryArg)!;
            var take = r.GetValue(takeOpt);

            var moments = await ctx.MomentStore.ListRecentAsync(take, ct);
            var archive = await ctx.ArchiveStore.ListRecentAsync(take, reason: null, ct);

            var results = new List<object>();
            foreach (var m in moments.Where(x =>
                x.MomentId.Contains(query, StringComparison.OrdinalIgnoreCase)))
            {
                var c = await ctx.MomentClassifierStore.GetAsync(m.MomentId, ct);
                results.Add(new
                {
                    Source = "moment",
                    Id = m.MomentId,
                    Detail = $"Load:{m.CognitiveLoad:F2} Arousal:{m.Arousal:F2}",
                    Category = c?.Category.ToString() ?? "",
                    Time = m.StartedAt.ToString("HH:mm:ss")
                });
            }
            foreach (var a in archive.Where(x =>
                x.OriginalId.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (x.Reason ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)))
            {
                results.Add(new
                {
                    Source = "archive",
                    Id = a.OriginalId,
                    Detail = a.Reason ?? "",
                    Category = "",
                    Time = a.ForgottenAt.ToString("HH:mm:ss")
                });
            }

            renderer.RenderTable(results.Take(take).ToList(), "Source", "Id", "Detail", "Category", "Time");
            return 0;
        });
        cmd.Add(search);

        var working = new Command("working", "Show working memory state");
        working.SetAction((ParseResult _, CancellationToken _) =>
        {
            var state = ctx.Homeostasis.GetState();
            renderer.RenderStatus(state, "Running",
                ctx.ExecutiveController.CurrentState.Flags.ToString(),
                0, 0, 0, "N/A", "N/A");
            return Task.FromResult(0);
        });
        cmd.Add(working);

        return cmd;
    }
}