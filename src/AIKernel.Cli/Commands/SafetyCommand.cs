using System.CommandLine;
using AIKernel.Cli.Services;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class SafetyCommand(ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("safety", "Safety audit and rules");
        cmd.SetAction((ParseResult _, CancellationToken _) =>
        {
            renderer.Console.MarkupLine("[yellow]Safety service not available[/]");
            return Task.FromResult(0);
        });
        return cmd;
    }
}
