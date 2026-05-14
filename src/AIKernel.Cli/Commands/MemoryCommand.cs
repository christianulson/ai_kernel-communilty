using System.CommandLine;
using AIKernel.Cli.Services;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class MemoryCommand(ConsoleRenderer renderer)
{
    public Command Build()
    {
        var query = new Argument<string>("query")
        {
            Description = "Search query"
        };
        var cmd = new Command("memory", "Search cognitive memory")
        {
            query
        };
        cmd.SetAction((ParseResult _, CancellationToken _) =>
        {
            renderer.Console.MarkupLine("[yellow]Memory search requires ICognitiveMemory service[/]");
            return Task.FromResult(0);
        });
        return cmd;
    }
}
