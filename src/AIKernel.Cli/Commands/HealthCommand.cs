using System.CommandLine;
using AIKernel.Cli.Services;

namespace AIKernel.Cli.Commands;

public sealed class HealthCommand(CliContext ctx, ConsoleRenderer renderer)
{
    private sealed record ModuleHealth(string Name, string Status, string Latency);

    public Command Build()
    {
        var cmd = new Command("health", "Check module health");
        cmd.SetAction((ParseResult _, CancellationToken ct) =>
        {
            var modules = new[]
            {
                Check("MomentStore", () => { ctx.MomentStore.ListRecentAsync(1, ct).GetAwaiter().GetResult(); }),
                Check("SnapshotService", () => { ctx.SnapshotService.ListSnapshotsAsync(null, ct).GetAwaiter().GetResult(); }),
                Check("Anticipation", () => { ctx.AnticipationService.GetActiveProjectionsAsync(null, ct).GetAwaiter().GetResult(); }),
                Check("ProspectiveMemory", () => { ctx.ProspectiveMemory.GetPendingIntentionsAsync(ct).GetAwaiter().GetResult(); }),
                Check("ExecutiveController", () => { var s = ctx.ExecutiveController.CurrentState; }),
            };
            var failures = modules.Count(m => m.Status != "ok");
            var overall = failures == 0 ? "OK" : $"DEGRADED ({failures} failure(s))";
            renderer.RenderHealth([.. modules], overall);
            return Task.FromResult(failures > 0 ? 1 : 0);
        });
        return cmd;
    }

    private static ModuleHealth Check(string name, Action check)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            check();
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
