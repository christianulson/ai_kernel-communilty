using System.CommandLine;
using KrnlAI.Embedded;
using KrnlAI.Cli.Tui;

namespace KrnlAI.Cli.Commands;

public sealed class InteractiveCommand
{
    public Command Build()
    {
        var endpoint = new Option<string>("--endpoint")
        {
            Description = "URL do backend Krnl-AI",
            DefaultValueFactory = _ => "http://localhost:5235"
        };
        var local = new Option<bool>("--local") { Description = "Use EmbeddedKrnlAI in-process" };
        var mode = new Option<string>("--mode")
        {
            Description = "Runtime mode: embedded, local-api, remote-api",
            DefaultValueFactory = _ => "remote-api"
        };
        var model = new Option<string>("--model") { Description = "LLM provider for local mode", DefaultValueFactory = _ => "ollama" };
        var cmd = new Command("chat", "Modo interativo (TUI) no terminal")
        {
            endpoint,
            local,
            mode,
            model
        };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var runtimeMode = r.GetValue(mode) ?? "remote-api";
            var useLocal = r.GetValue(local) || string.Equals(runtimeMode, "embedded", StringComparison.OrdinalIgnoreCase);
            var engine = useLocal
                ? new TuiEngine(new EmbeddedKrnlAI(new EmbeddedKernelOptions { LLmProvider = r.GetValue(model) ?? "ollama" }))
                : new TuiEngine(r.GetValue(endpoint) ?? "http://localhost:5235");
            try
            {
                await engine.RunAsync(ct).ConfigureAwait(false);
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
