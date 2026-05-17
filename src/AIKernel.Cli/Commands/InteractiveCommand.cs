using System.CommandLine;
using AIKernel.Cli.Tui;

namespace AIKernel.Cli.Commands;

public sealed class InteractiveCommand
{
    public Command Build()
    {
        var endpoint = new Option<string>("--endpoint", "URL do backend AI Kernel")
        {
            DefaultValueFactory = _ => "http://localhost:5000"
        };
        var cmd = new Command("chat", "Modo interativo (TUI) no terminal")
        {
            endpoint
        };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var url = r.GetValue(endpoint) ?? "http://localhost:5000";
            var engine = new TuiEngine(url);
            try
            {
                await engine.RunAsync(ct);
            }
            finally
            {
                engine.Dispose();
            }
            return 0;
        });
        return cmd;
    }
}
