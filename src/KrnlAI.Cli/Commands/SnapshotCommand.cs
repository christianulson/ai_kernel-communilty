using System.CommandLine;
using KrnlAI.Cli.Services;
using Kernel.Core.Services.Memory;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class SnapshotCommand(CliContext ctx, ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("snapshot", "Manage snapshots");

        var list = new Command("list", "List snapshots");
        list.SetAction(async (ParseResult _, CancellationToken ct) =>
        {
            var snapshots = await ctx.SnapshotService.ListSnapshotsAsync(null, ct);
            if (snapshots.Count == 0)
            {
                renderer.Console.MarkupLine("[yellow]No snapshots[/]");
                return 0;
            }
            var rows = snapshots.Select(s => new
            {
                s.Id.Value,
                s.Label,
                Scope = s.Scope.ToString(),
                s.Reason,
                Created = s.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                Components = s.ComponentList.Count.ToString()
            }).ToList();
            renderer.RenderTable(rows, "Value", "Label", "Scope", "Reason", "Created", "Components");
            return 0;
        });
        cmd.Add(list);

        var labelOpt = new Option<string>("--label")
        {
            Description = "Snapshot label", DefaultValueFactory = _ => $"cli-snapshot-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}"
        };
        var scopeOpt = new Option<string>("--scope")
        {
            Description = "Snapshot scope (Full, WorkingMemory, etc.)",
            DefaultValueFactory = _ => "Full"
        };
        var reasonOpt = new Option<string>("--reason")
        {
            Description = "Reason for snapshot", DefaultValueFactory = _ => "manual"
        };
        var create = new Command("create", "Create a snapshot")
        {
            labelOpt, scopeOpt, reasonOpt
        };
        create.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var label = r.GetValue(labelOpt)!;
            var scopeStr = r.GetValue(scopeOpt)!;
            var reason = r.GetValue(reasonOpt)!;

            if (!Enum.TryParse<SnapshotScope>(scopeStr, ignoreCase: true, out var scope))
            {
                renderer.Console.MarkupLine($"[red]Invalid scope '{scopeStr}'. Valid values: {string.Join(", ", Enum.GetNames<SnapshotScope>())}[/]");
                return 1;
            }

            var snapshot = await ctx.SnapshotService.CreateSnapshotAsync(label, scope, reason, ct);
            renderer.Console.MarkupLine($"[green]Snapshot created:[/] {snapshot.Id.Value}");
            renderer.Console.MarkupLine($"  Label: {snapshot.Label}");
            renderer.Console.MarkupLine($"  Scope: {snapshot.Scope}");
            renderer.Console.MarkupLine($"  Components: {snapshot.ComponentList.Count}");
            return 0;
        });
        cmd.Add(create);

        var idArg = new Argument<string>("id") { Description = "Snapshot ID" };
        var restore = new Command("restore", "Restore a snapshot") { idArg };
        restore.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var id = new SnapshotId(r.GetValue(idArg)!);
            var result = await ctx.SnapshotService.RestoreSnapshotAsync(id, null, ct);
            if (result.Success)
            {
                renderer.Console.MarkupLine($"[green]Snapshot restored:[/] {result.Id.Value} ({result.Duration.TotalMilliseconds:F0}ms)");
                renderer.Console.MarkupLine($"  Restored: {string.Join(", ", result.RestoredComponents)}");
                return 0;
            }
            renderer.Console.MarkupLine($"[red]Restore failed:[/] {string.Join(", ", result.FailedComponents)}");
            return 1;
        });
        cmd.Add(restore);

        var delete = new Command("delete", "Delete a snapshot") { idArg };
        delete.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var id = new SnapshotId(r.GetValue(idArg)!);
            await ctx.SnapshotService.DeleteSnapshotAsync(id, ct);
            renderer.Console.MarkupLine($"[green]Snapshot deleted:[/] {id.Value}");
            return 0;
        });
        cmd.Add(delete);

        return cmd;
    }
}