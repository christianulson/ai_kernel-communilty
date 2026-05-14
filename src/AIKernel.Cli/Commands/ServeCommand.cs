using System.CommandLine;

namespace AIKernel.Cli.Commands;

public sealed class ServeCommand
{
    public Command Build()
    {
        var cmd = new Command("serve", "Start headless HTTP server");
        cmd.SetAction((ParseResult _, CancellationToken _) =>
        {
            Console.Error.WriteLine("Serve mode not yet implemented");
            return Task.FromResult(1);
        });
        return cmd;
    }
}
