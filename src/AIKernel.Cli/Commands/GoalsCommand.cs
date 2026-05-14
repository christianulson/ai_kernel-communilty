using System.CommandLine;
using AIKernel.Cli.Services;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class GoalsCommand(ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("goals", "Manage goals");
        cmd.SetAction((ParseResult _, CancellationToken _) =>
        {
            renderer.Console.MarkupLine("[yellow]Goals service not available[/]");
            return Task.FromResult(0);
        });
        return cmd;
    }
}
