using System.CommandLine;
using AIKernel.Cli.Services;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class ArchiveCommand(ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("archive", "Manage archives");
        cmd.SetAction((ParseResult _, CancellationToken _) =>
        {
            renderer.Console.MarkupLine("[yellow]Archive service not available[/]");
            return Task.FromResult(0);
        });
        return cmd;
    }
}
