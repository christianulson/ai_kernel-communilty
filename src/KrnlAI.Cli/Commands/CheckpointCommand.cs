using System.CommandLine;
using KrnlAI.Cli.Services;
using KrnlAI.Core.Abstractions;

namespace KrnlAI.Cli.Commands;

public sealed class CheckpointCommand(CliContext ctx, ConsoleRenderer renderer)
{
    private static string SessionId => $"cli-{Environment.ProcessId}";

    public Command Build()
    {
        var cmd = new Command("checkpoint", "Manage git checkpoints");

        // list
        var listCmd = new Command("list", "List checkpoints for current session");
        listCmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var mgr = ctx.GetService<ICheckpointManager>();
            var list = await mgr.ListCheckpointsAsync(SessionId, ct);
            renderer.RenderCheckpointList(list);
        });
        cmd.Add(listCmd);

        // create <label>
        var labelArg = new Argument<string>("label") { Description = "Checkpoint label" };
        var createCmd = new Command("create", "Create a new checkpoint") { labelArg };
        createCmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var label = r.GetValue(labelArg) ?? "manual";
            var mgr = ctx.GetService<ICheckpointManager>();
            var id = await mgr.CreateCheckpointAsync(SessionId, label, ct);
            renderer.RenderSuccess($"Checkpoint created: {id[..8]}... ({label})");
        });
        cmd.Add(createCmd);

        // restore <id>
        var idArg = new Argument<string>("id") { Description = "Checkpoint ID" };
        var restoreCmd = new Command("restore", "Restore from a checkpoint") { idArg };
        restoreCmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var id = r.GetValue(idArg)!;
            var mgr = ctx.GetService<ICheckpointManager>();
            var ok = await mgr.RestoreFilesAsync(id, ct);
            renderer.RenderSuccess(ok ? "Files restored from checkpoint" : "Checkpoint not found");
        });
        cmd.Add(restoreCmd);

        // diff <id>
        var diffCmd = new Command("diff", "Show diff of a checkpoint") { idArg };
        diffCmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var id = r.GetValue(idArg)!;
            var mgr = ctx.GetService<ICheckpointManager>();
            var diff = await mgr.GetDiffAsync(id, ct);
            renderer.RenderDiff(diff ?? "Checkpoint not found");
        });
        cmd.Add(diffCmd);

        return cmd;
    }
}
