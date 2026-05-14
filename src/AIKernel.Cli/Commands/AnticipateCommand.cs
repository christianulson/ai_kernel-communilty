using System.CommandLine;
using AIKernel.Cli.Services;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class AnticipateCommand(ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("anticipate", "Show active projections");
        cmd.SetAction((ParseResult _, CancellationToken _) =>
        {
            renderer.Console.MarkupLine("[yellow]Anticipation service not available[/]");
            return Task.FromResult(0);
        });
        return cmd;
    }
}
