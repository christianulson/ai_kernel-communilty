using System.CommandLine;
using KrnlAI.Cli.Services;
using KrnlAI.Core.Services.Memory;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class MemoryCommand(CliContext ctx, ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("memory", "Search cognitive memory");

        var search = new Command("search", "Search memory by query");
        var queryArg = new Argument<string[]>("query") { Description = "Search query (optional)", Arity = ArgumentArity.ZeroOrMore };
        var takeOpt = new Option<int>("--take")
        {
            Description = "Max results", DefaultValueFactory = _ => 10
        };
        var categoryOpt = new Option<string>("--category")
        {
            Description = "Filter by moment category (Routine, Anomaly, Learning, Conflict, etc.)"
        };
        var domainOpt = new Option<string>("--domain")
        {
            Description = "Filter by focus domain"
        };

        search.Add(queryArg);
        search.Add(takeOpt);
        search.Add(categoryOpt);
        search.Add(domainOpt);

        search.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var query = r.GetValue(queryArg) is { Length: > 0 } q ? string.Join(" ", q) : string.Empty;
            var take = r.GetValue(takeOpt);
            var category = r.GetValue(categoryOpt);
            var domain = r.GetValue(domainOpt);

            var moments = await ctx.MomentStore.ListRecentAsync(take * 2, ct);

            if (!string.IsNullOrEmpty(category))
            {
                var ids = moments.Select(m => m.MomentId).ToList();
                var classifications = await ctx.MomentClassifierStore.GetBatchAsync(ids, ct);
                var filteredIds = classifications
                    .Where(c => c.Category.ToString().Equals(category, StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.MomentId)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                moments = [.. moments.Where(m => filteredIds.Contains(m.MomentId))];
            }

            if (!string.IsNullOrEmpty(domain))
            {
                moments = [.. moments.Where(m =>
                    m.FocusDomain?.ToString().Contains(domain, StringComparison.OrdinalIgnoreCase) == true)];
            }

            if (!string.IsNullOrEmpty(query))
            {
                moments = [.. moments.Where(m =>
                    m.MomentId.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (m.FocusDomain?.ToString() ?? "").Contains(query, StringComparison.OrdinalIgnoreCase))];
            }

            var classificationLookup = new Dictionary<string, MomentClassification>();
            if (moments.Count > 0)
            {
                var classifications = await ctx.MomentClassifierStore.GetBatchAsync(
                    [.. moments.Select(m => m.MomentId)], ct);
                foreach (var c in classifications)
                    classificationLookup[c.MomentId] = c;
            }

            var results = new List<object>();
            foreach (var m in moments.Take(take))
            {
                classificationLookup.TryGetValue(m.MomentId, out var cls);
                results.Add(new
                {
                    Id = m.MomentId,
                    Category = cls?.Category.ToString() ?? "",
                    Domain = m.FocusDomain?.ToString() ?? "",
                    Load = $"{m.CognitiveLoad:F2}",
                    Arousal = $"{m.Arousal:F2}",
                    Valence = $"{m.Valence:F2}",
                    Time = m.StartedAt.ToString("HH:mm:ss")
                });
            }

            renderer.RenderTable(results, "Id", "Category", "Domain", "Load", "Arousal", "Valence", "Time");
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
