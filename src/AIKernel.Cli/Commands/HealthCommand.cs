using System.CommandLine;
using KrnlAI.Cli.Services;

namespace KrnlAI.Cli.Commands;

public sealed class HealthCommand(CliContext ctx, ConsoleRenderer renderer)
{
    private sealed record ModuleHealth(string Name, string Status, string Latency);

    public Command Build()
    {
        var cmd = new Command("health", "Check module health");
        cmd.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            var modules = await Task.WhenAll(
                CheckAsync("MomentStore", async () => { await ctx.MomentStore.ListRecentAsync(1, ct); }),
                CheckAsync("SnapshotService", async () => { await ctx.SnapshotService.ListSnapshotsAsync(null, ct); }),
                CheckAsync("Anticipation", async () => { await ctx.AnticipationService.GetActiveProjectionsAsync(null, ct); }),
                CheckAsync("ProspectiveMemory", async () => { await ctx.ProspectiveMemory.GetPendingIntentionsAsync(ct); }),
                CheckAsync("ExecutiveController", async () => { var s = ctx.ExecutiveController.CurrentState; await Task.CompletedTask; })
            );
            var failures = modules.Count(m => m.Status != "ok");
            var overall = failures == 0 ? "OK" : $"DEGRADED ({failures} failure(s))";
            renderer.RenderHealth([.. modules], overall);
            return failures > 0 ? 1 : 0;
        });
        return cmd;
    }

    private static async Task<ModuleHealth> CheckAsync(string name, Func<Task> check)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await check();
            sw.Stop();
            return new ModuleHealth(name, "ok", $"{sw.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new ModuleHealth(name, $"fail: {ex.GetType().Name}", $"{sw.ElapsedMilliseconds}ms");
        }
    }
}
