using System.CommandLine;
using KrnlAI.Cli.Services;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class DebugCommand(CliContext ctx, ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("debug", "Diagnose kernel components");
        cmd.SetAction(async (ParseResult _, CancellationToken ct) =>
        {
            renderer.Console.MarkupLine("[underline]Kernel Diagnostics[/]\n");

            var checks = new List<(string Name, bool Ok, string Detail)>();

            try
            {
                var state = ctx.Homeostasis.GetState();
                checks.Add(("CognitiveHomeostasis", true, $"Load:{state.Fatigue + state.StarvationForNovelty + state.SleepPressure:F2} Health:{state.HealthScore:F2}"));
            }
            catch (Exception ex) { checks.Add(("CognitiveHomeostasis", false, ex.Message)); }

            try
            {
                var exec = ctx.ExecutiveController.CurrentState;
                checks.Add(("ExecutiveController", true, $"Flags:{exec.Flags}"));
            }
            catch (Exception ex) { checks.Add(("ExecutiveController", false, ex.Message)); }

            try
            {
                var moments = await ctx.MomentStore.ListRecentAsync(1, ct);
                checks.Add(("MomentStore", true, $"{moments.Count} recent"));
            }
            catch (Exception ex) { checks.Add(("MomentStore", false, ex.Message)); }

            try
            {
                var archives = await ctx.ArchiveStore.CountArchivedAsync(ct);
                checks.Add(("ArchiveStore", true, $"{archives} entries"));
            }
            catch (Exception ex) { checks.Add(("ArchiveStore", false, ex.Message)); }

            try
            {
                var snapshots = await ctx.SnapshotService.ListSnapshotsAsync(null, ct);
                checks.Add(("SnapshotService", true, $"{snapshots.Count} snapshots"));
            }
            catch (Exception ex) { checks.Add(("SnapshotService", false, ex.Message)); }

            try
            {
                var projections = await ctx.AnticipationService.GetActiveProjectionsAsync(null, ct);
                checks.Add(("AnticipationService", true, $"{projections.Count} projections"));
            }
            catch (Exception ex) { checks.Add(("AnticipationService", false, ex.Message)); }

            try
            {
                var intentions = await ctx.ProspectiveMemory.GetPendingIntentionsAsync(ct);
                checks.Add(("ProspectiveMemory", true, $"{intentions.Count} intentions"));
            }
            catch (Exception ex) { checks.Add(("ProspectiveMemory", false, ex.Message)); }

            try
            {
                var goals = await ctx.GoalStore.ListActiveAsync(null, ct);
                checks.Add(("GoalStore", true, $"{goals.Count} active goals"));
            }
            catch (Exception ex) { checks.Add(("GoalStore", false, ex.Message)); }

            try
            {
                var rules = ctx.RulesEngine.GetAllRules();
                checks.Add(("FundamentalRulesEngine", true, $"{rules.Count} rules"));
            }
            catch (Exception ex) { checks.Add(("FundamentalRulesEngine", false, ex.Message)); }

            try
            {
                var records = await ctx.SafetyCaseStore.ListRecentAsync(1, ct);
                checks.Add(("SafetyCaseStore", true, $"{records.Count} records"));
            }
            catch (Exception ex) { checks.Add(("SafetyCaseStore", false, ex.Message)); }

            var failed = checks.Count(c => !c.Ok);
            foreach (var (name, ok, detail) in checks)
            {
                var icon = ok ? "[green]PASS[/]" : "[red]FAIL[/]";
                renderer.Console.MarkupLine($"{icon} [bold]{name}[/] {detail}");
            }

            renderer.Console.MarkupLine(failed == 0
                ? $"\n[green]All {checks.Count} components healthy[/]"
                : $"\n[red]{failed}/{checks.Count} component(s) failed[/]");

            return failed > 0 ? 1 : 0;
        });
        return cmd;
    }
}