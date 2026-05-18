using System.CommandLine;
using KrnlAI.Embedded;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class ServeCommand
{
    public Command Build()
    {
        var portOpt = new Option<int>("--port")
        {
            Description = "HTTP port", DefaultValueFactory = _ => 5100
        };
        var modelOpt = new Option<string>("--model")
        {
            Description = "LLM provider for local embedded mode",
            DefaultValueFactory = _ => "ollama"
        };
        var cmd = new Command("serve", "Start headless HTTP server") { portOpt, modelOpt };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var port = r.GetValue(portOpt);
            var model = r.GetValue(modelOpt) ?? "ollama";
            var kernel = new EmbeddedKernel(new EmbeddedKernelOptions { LLmProvider = model });

            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
            builder.Logging.ClearProviders();
            builder.Logging.AddFilter("Microsoft", LogLevel.Warning);

            var app = builder.Build();

            app.MapPost("/agent/run", async (AgentRunRequest req, CancellationToken requestCt) =>
            {
                var result = await kernel.RunAsync(req.Prompt ?? req.Input ?? string.Empty, requestCt);
                return Results.Ok(new { result.Narration, result.Error, result.Steps, result.Mode });
            });

            app.MapPost("/memory/search", async (MemorySearchRequest req, CancellationToken requestCt) =>
            {
                var hits = await kernel.SearchMemoryAsync(req.Query, requestCt);
                return Results.Ok(new { hits, totalCount = hits.Count, mode = "community" });
            });

            app.MapGet("/health", () => Results.Ok(new
            {
                status = "ok",
                mode = "community",
                llm = model,
                timestamp = DateTimeOffset.UtcNow
            }));

            app.MapGet("/status", () => Results.Ok(new
            {
                service = "Krnl-AI CLI",
                version = "1.0.0",
                uptime = Environment.TickCount64 / 1000,
                mode = "community"
            }));

            AnsiConsole.MarkupLine($"[green]Krnl-AI CLI serving on http://localhost:{port} (press Ctrl+C to stop)[/]");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                AnsiConsole.MarkupLine("\n[yellow]Shutdown requested...[/]");
                cts.Cancel();
            };

            await app.StartAsync(cts.Token);
            var host = (IHost)app;
            await host.WaitForShutdownAsync(cts.Token);
            await kernel.DisposeAsync();
            return 0;
        });
        return cmd;
    }

    private sealed record AgentRunRequest(string? Prompt, string? Input);
    private sealed record MemorySearchRequest(string Query);
}
