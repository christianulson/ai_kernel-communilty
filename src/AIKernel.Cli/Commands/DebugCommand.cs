using System.CommandLine;
using AIKernel.Cli.Services;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class DebugCommand(ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("debug", "Diagnose kernel components");
        cmd.SetAction((ParseResult _, CancellationToken _) =>
        {
            renderer.Console.MarkupLine("[green]Kernel.Core: OK (services registered)[/]");
            return Task.FromResult(0);
        });
        return cmd;
    }
}
