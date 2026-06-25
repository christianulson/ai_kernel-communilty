using System.CommandLine;
using KrnlAI.Cli.Services;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class IntentionsCommand(CliContext ctx, ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("intentions", "List pending intentions");
        var domainOpt = new Option<string>("--domain")
        {
            Description = "Filter by domain"
        };
        cmd.Add(domainOpt);
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var domain = r.GetValue(domainOpt);
            var intentions = await ctx.ProspectiveMemory.GetPendingIntentionsAsync(ct).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(domain))
                intentions = [.. intentions.Where(i =>
                    i.Domain != null && i.Domain.Contains(domain, StringComparison.OrdinalIgnoreCase))];

            if (intentions.Count == 0)
            {
                renderer.Console.MarkupLine("[yellow]No pending intentions[/]");
                return 0;
            }

            var rows = intentions.Select(i => new
            {
                i.IntentionId,
                Desc = (i.Description.Length > 55 ? i.Description[..55] + "..." : i.Description),
                Domain = i.Domain ?? "*",
                Trigger = i.Trigger.Type.ToString(),
                Priority = $"{i.Priority:F2}",
                Status = i.Status.ToString(),
                Created = i.CreatedAt.ToString("MM-dd HH:mm")
            }).ToList();
            renderer.RenderTable(rows, "IntentionId", "Desc", "Domain", "Trigger", "Priority", "Status", "Created");
            return 0;
        });
        return cmd;
    }
}