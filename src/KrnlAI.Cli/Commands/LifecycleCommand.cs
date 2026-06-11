using System.CommandLine;
using KrnlAI.Cli.Services;
using KrnlAI.Core.Abstractions;
using KrnlAI.Core.Services.Lifecycle;

namespace KrnlAI.Cli.Commands;

public sealed class LifecycleCommand(LifecycleOrchestrator orchestrator, ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("lifecycle", "Manage lifecycle hooks");

        var listCmd = new Command("list", "List registered lifecycle hooks");
        listCmd.SetAction((_, _) =>
        {
            var hooks = orchestrator.ListHooks();
            var items = hooks.Select(h => new HookListItem(h.EventType.ToString(), h.GetType().Name, h.Priority.ToString())).ToList();
            renderer.RenderTable(items, "EventType", "HookType", "Priority");
            return Task.FromResult(0);
        });

        var eventArg = new Option<LifecycleEventType>("--event") { Description = "Event type to execute" };
        var runCmd = new Command("run", "Execute lifecycle hooks for an event") { eventArg };
        runCmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var eventType = r.GetValue(eventArg);
            var context = new LifecycleContext(
                EventType: eventType,
                ProjectPath: "krnlai.slnx",
                Configuration: "Debug",
                Environment: new Dictionary<string, string>());
            var result = await orchestrator.ExecuteAsync(eventType, context, ct);
            if (result.Success)
            {
                renderer.RenderSuccess("Lifecycle hooks executed successfully");
            }
            else
            {
                renderer.RenderError($"Lifecycle hooks failed: {result.Error}");
                return 1;
            }
            return 0;
        });

        cmd.Add(listCmd);
        cmd.Add(runCmd);
        return cmd;
    }

    private sealed record HookListItem(string EventType, string HookType, string Priority);
}
