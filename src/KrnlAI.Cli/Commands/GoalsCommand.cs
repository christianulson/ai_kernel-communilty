using System.CommandLine;
using KrnlAI.Cli.Services;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class GoalsCommand(CliContext ctx, ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("goals", "Manage goals");

        var list = new Command("list", "List active goals");
        list.SetAction(async (ParseResult _, CancellationToken ct) =>
        {
            var goals = await ctx.GoalStore.ListActiveAsync(null, ct);
            if (goals.Count == 0)
            {
                renderer.Console.MarkupLine("[yellow]No active goals[/]");
                return 0;
            }
            var rows = goals.Select(g => new
            {
                g.GoalId,
                Desc = g.Description.Length > 60 ? g.Description[..60] + "..." : g.Description,
                g.Status,
                Progress = $"{g.Progress:P0}",
                g.Priority,
                Created = g.CreatedAt.ToString("yyyy-MM-dd"),
                Deadline = g.Deadline?.ToString("yyyy-MM-dd") ?? "none"
            }).ToList();
            renderer.RenderTable(rows, "GoalId", "Desc", "Status", "Progress", "Priority", "Created", "Deadline");
            return 0;
        });
        cmd.Add(list);

        var idArg = new Argument<string>("id") { Description = "Goal ID" };
        var get = new Command("get", "Show goal details") { idArg };
        get.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var id = r.GetValue(idArg)!;
            var goal = await ctx.GoalStore.GetAsync(id, ct);
            if (goal is null)
            {
                renderer.Console.MarkupLine($"[red]Goal '{id}' not found[/]");
                return 1;
            }
            renderer.Console.MarkupLine($"[bold]GoalId:[/] {goal.GoalId}");
            renderer.Console.MarkupLine($"[bold]Description:[/] {goal.Description}");
            renderer.Console.MarkupLine($"[bold]Status:[/] {goal.Status}");
            renderer.Console.MarkupLine($"[bold]Progress:[/] {goal.Progress:P0}");
            renderer.Console.MarkupLine($"[bold]Priority:[/] {goal.Priority}");
            renderer.Console.MarkupLine($"[bold]Created:[/] {goal.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            renderer.Console.MarkupLine($"[bold]Deadline:[/] {goal.Deadline?.ToString("yyyy-MM-dd HH:mm:ss") ?? "none"}");
            renderer.Console.MarkupLine($"[bold]ParentGoalId:[/] {goal.ParentGoalId ?? "none"}");
            renderer.Console.MarkupLine($"[bold]SubGoals:[/] {goal.SubGoalIds.Count}");
            renderer.Console.MarkupLine($"[bold]DependsOn:[/] {goal.DependsOnGoalIds.Count}");
            return 0;
        });
        cmd.Add(get);
        cmd.Add(new KanbanCommand(ctx, new KanbanRenderer(renderer.Console)).Build());

        return cmd;
    }
}