using System.CommandLine;
using AIKernel.Embedded;
using AIKernel.Cli.Tui;

namespace AIKernel.Cli.Commands;

public sealed class InteractiveCommand
{
    public Command Build()
    {
        var endpoint = new Option<string>("--endpoint")
        {
            Description = "URL do backend AI Kernel",
            DefaultValueFactory = _ => "http://localhost:5000"
        };
        var local = new Option<bool>("--local") { Description = "Use EmbeddedKernel in-process" };
        var model = new Option<string>("--model") { Description = "LLM provider for local mode", DefaultValueFactory = _ => "ollama" };
        var cmd = new Command("chat", "Modo interativo (TUI) no terminal")
        {
            endpoint,
            local,
            model
        };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var useLocal = r.GetValue(local);
            var engine = useLocal
                ? new TuiEngine(new EmbeddedKernel(new EmbeddedKernelOptions { LLmProvider = r.GetValue(model) ?? "ollama" }))
                : new TuiEngine(r.GetValue(endpoint) ?? "http://localhost:5000");
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
