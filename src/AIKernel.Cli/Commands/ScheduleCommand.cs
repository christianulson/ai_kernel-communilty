using System.CommandLine;
using AIKernel.Cli.Services;
using Kernel.Core.Abstractions;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class ScheduleCommand(CliContext ctx, IAnsiConsole console)
{
    public Command Build()
    {
        var cmd = new Command("schedule", "Schedule a local kernel action");
        var textArg = new Argument<string>("description") { Description = "Task description" };
        var atOpt = new Option<DateTimeOffset?>("--at") { Description = "Execution date/time. Defaults to now." };
        cmd.Add(textArg);
        cmd.Add(atOpt);

        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var description = r.GetValue(textArg) ?? "scheduled action";
            var scheduledAt = r.GetValue(atOpt) ?? DateTimeOffset.UtcNow;
            var action = new ScheduledAction(Guid.NewGuid().ToString("N"), description, scheduledAt, "{}", "cli");

            await ctx.Scheduler.ScheduleAsync(action, ct);
            console.MarkupLine($"[green]Scheduled[/] {action.ActionId} at {scheduledAt:O}");
            return 0;
        });

        return cmd;
    }
}
