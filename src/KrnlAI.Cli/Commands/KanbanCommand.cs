using System.CommandLine;
using KrnlAI.Cli.Services;
using KrnlAI.LLMGateway.Core.Abstractions;


namespace KrnlAI.Cli.Commands;

public sealed class KanbanCommand(CliContext ctx, KanbanRenderer renderer)
{
    /// <summary>Builds the kanban CLI command with options and handler.</summary>
    public Command Build()
    {
        var daysOpt = new Option<int>("--days")
        {
            Description = "Days back for completed/failed goals",
            DefaultValueFactory = _ => 10
        };
        var domainOpt = new Option<string?>("--domain")
        {
            Description = "Filter by domain"
        };
        var priorityOpt = new Option<double?>("--min-priority")
        {
            Description = "Minimum priority"
        };
        var searchOpt = new Option<string?>("--search")
        {
            Description = "Search in description"
        };

        var cmd = new Command("kanban", "Show Kanban board of goals")
        {
            daysOpt, domainOpt, priorityOpt, searchOpt
        };

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var days = r.GetValue(daysOpt);
            var domain = r.GetValue(domainOpt);
            var priority = r.GetValue(priorityOpt);
            var search = r.GetValue(searchOpt);

            try
            {
                var svc = ctx.GetService<IKanbanService>();
                var data = await svc.GetKanbanAsync(days, domain, priority, search: search, ct: ct);
                renderer.Render(data);
                return 0;
            }
            catch (Exception ex)
            {
                renderer.OutputError($"[red]Error:[/] {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        });

        return cmd;
    }
}
