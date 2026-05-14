using System.CommandLine;
using AIKernel.Cli.Services;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class IntentionsCommand(ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("intentions", "List pending intentions");
        cmd.SetAction((ParseResult _, CancellationToken _) =>
        {
            renderer.Console.MarkupLine("[yellow]Prospective memory service not available[/]");
            return Task.FromResult(0);
        });
        return cmd;
    }
}
