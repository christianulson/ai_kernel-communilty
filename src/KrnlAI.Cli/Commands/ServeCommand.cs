using System.CommandLine;
using KrnlAI.Embedded;
using KrnlAI.Core.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
        var apiKeyOpt = new Option<string?>("--api-key")
        {
            Description = "API key for request authentication (optional)"
        };
        var cmd = new Command("serve", "Start headless HTTP server") { portOpt, modelOpt, apiKeyOpt };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var port = r.GetValue(portOpt);
            var model = r.GetValue(modelOpt) ?? "ollama";
            var apiKey = r.GetValue(apiKeyOpt);
            var kernel = new EmbeddedKrnlAI(new EmbeddedKernelOptions { LLmProvider = model });

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
            builder.Logging.ClearProviders();
            builder.Logging.AddFilter("Microsoft", LogLevel.Warning);

            builder.Services.AddCors();
            builder.Services.AddSignalR();
            builder.Services.AddRateLimiter(_ => { });
            builder.Services.AddHealthChecks();

            builder.Services.AddSingleton(kernel);

            var app = builder.Build();

            app.UseCors(c => c.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            app.Use(async (ctx, next) =>
            {
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    if (!ctx.Request.Headers.TryGetValue("X-API-Key", out var key) || key != apiKey)
                    {
                        ctx.Response.StatusCode = 401;
                        await ctx.Response.WriteAsync("Unauthorized", ct);
                        return;
                    }
                }
                await next(ctx);
            });

            app.MapPost("/agent/run", async (AgentRunRequest req, CancellationToken requestCt) =>
            {
                var result = await kernel.RunAsync(req.Prompt ?? req.Input ?? string.Empty, requestCt);
                return Results.Ok(new { result.Narration, result.Error, result.Steps, result.Mode });
            });

            app.MapPost("/agent/run-stream", async (AgentRunRequest req, HttpContext ctx, CancellationToken requestCt) =>
            {
                ctx.Response.ContentType = "text/event-stream";
                ctx.Response.Headers.CacheControl = "no-cache";
                ctx.Response.Headers.Connection = "keep-alive";
                var result = await kernel.RunAsync(req.Prompt ?? req.Input ?? string.Empty, requestCt);
                await ctx.Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { result.Narration, result.Error, result.Steps, result.Mode })}\n\n", requestCt);
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

            app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));
            app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));

            app.MapGet("/status", () => Results.Ok(new
            {
                service = "Krnl-AI CLI",
                version = "1.0.0",
                uptime = Environment.TickCount64 / 1000,
                mode = "community"
            }));

            app.MapPost("/agent/session/create", async (CreateSessionRequest req, CancellationToken requestCt) =>
            {
                var result = await kernel.RunAsync(req.InitialPrompt ?? string.Empty, requestCt);
                return Results.Ok(new { sessionId = Guid.NewGuid().ToString("N"), result.Narration, result.Mode });
            });

            app.MapGet("/agent/session/{sessionId}", (string sessionId) =>
                Results.Ok(new { sessionId, status = "active" }));

            app.MapHealthChecks("/healthz");

            AnsiConsole.MarkupLine($"[green]Krnl-AI CLI serving on http://localhost:{port} (press Ctrl+C to stop)[/]");
            if (!string.IsNullOrWhiteSpace(apiKey))
                AnsiConsole.MarkupLine("[yellow]API key authentication enabled[/]");

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
    private sealed record CreateSessionRequest(string? InitialPrompt);
}
