using System.CommandLine;
using KrnlAI.Cli.Services;

namespace KrnlAI.Cli.Commands;

public sealed class StatusCommand(CliContext ctx, ConsoleRenderer renderer)
{
    public Command Build()
    {
        var verbose = new Option<bool>("--verbose")
        {
            Description = "Show detailed state"
        };
        var cmd = new Command("status", "Show kernel status")
        {
            verbose
        };
        cmd.SetAction((ParseResult r, CancellationToken ct) =>
        {
            var state = ctx.Homeostasis.GetState();
            var mode = ctx.ExecutiveController.CurrentState.Flags.ToString();
            var isVerbose = r.GetValue(verbose);

            if (isVerbose)
            {
                renderer.RenderStatusVerbose(state, "Running", mode);
            }
            else
            {
                renderer.RenderStatus(state, "Running", mode, 0, 0, 0, "N/A", "N/A");
            }
            return Task.FromResult(0);
        });
        return cmd;
    }
}
