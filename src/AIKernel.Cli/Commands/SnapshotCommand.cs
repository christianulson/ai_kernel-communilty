using System.CommandLine;
using AIKernel.Cli.Services;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class SnapshotCommand(ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("snapshot", "Manage snapshots");
        cmd.SetAction((ParseResult _, CancellationToken _) =>
        {
            renderer.Console.MarkupLine("[yellow]Snapshot service not available[/]");
            return Task.FromResult(0);
        });
        return cmd;
    }
}
