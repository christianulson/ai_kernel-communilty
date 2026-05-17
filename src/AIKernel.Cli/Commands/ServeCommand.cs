using System.CommandLine;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace AIKernel.Cli.Commands;

public sealed class ServeCommand
{
    public Command Build()
    {
        var portOpt = new Option<int>("--port")
        {
            Description = "HTTP port", DefaultValueFactory = _ => 5100
        };
        var cmd = new Command("serve", "Start headless HTTP server") { portOpt };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var port = r.GetValue(portOpt);

            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
            builder.Logging.ClearProviders();
            builder.Logging.AddFilter("Microsoft", LogLevel.Warning);

            var app = builder.Build();

            app.MapGet("/health", () => Results.Ok(new
            {
                status = "ok",
                timestamp = DateTimeOffset.UtcNow
            }));

            app.MapGet("/status", () => Results.Ok(new
            {
                service = "AI Kernel CLI",
                version = "1.0.0",
                uptime = Environment.TickCount64 / 1000,
                mode = "headless"
            }));

            AnsiConsole.MarkupLine($"[green]AI Kernel CLI serving on http://localhost:{port} (press Ctrl+C to stop)[/]");

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
            return 0;
        });
        return cmd;
    }
}