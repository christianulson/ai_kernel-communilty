using Kernel.Abstractions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;

namespace AIKernel.Sidecar;

public static class EndpointRouteExtensions
{
    public static WebApplication MapSidecarEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (ctx, report) =>
            {
                ctx.Response.ContentType = "application/json";
                var json = System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    ts = DateTime.UtcNow,
                    version = "AIKernel.Sidecar/1.0.0",
                    checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() })
                });
                await ctx.Response.WriteAsync(json, cancellationToken: ctx.RequestAborted);
            }
        }).RequireRateLimiting("health");
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") })
            .RequireRateLimiting("health");
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = check => check.Tags.Contains("live") })
            .RequireRateLimiting("health");

        app.MapPost("/agent/run", async (HttpContext ctx, IAdversarialGuard guard, KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            AgentRunRequest? body;
            try { body = await ctx.Request.ReadFromJsonAsync<AgentRunRequest>(cancellationToken: ctx.RequestAborted); }
            catch { body = null; }
            if (body == null) return Results.BadRequest(new { error = "invalid_request" });

            var safetyResult = await guard.ValidateAsync(body.Prompt ?? "", ctx.RequestAborted);
            if (!safetyResult.IsAllowed)
            {
                logger.LogWarning("Agent run blocked. ThreatLevel={Threat}, Patterns={Patterns}", safetyResult.ThreatLevel, safetyResult.DetectedPatterns);
                return Results.Ok(new AgentRunResponse
                {
                    Narration = "Conteúdo bloqueado.",
                    Error = "safety_block",
                    TransportSteps = [new TransportStepDto { Label = "Segurança", Detail = "BLOQUEADO", Ok = false }],
                    ActiveStages = ["standalone"]
                });
            }

            var proxyResult = await kernel.ProxyPostAsync<AgentRunRequest, AgentRunResponse>("/v1/agent/run", body, ctx.RequestAborted);
            if (proxyResult != null) return Results.Ok(proxyResult);

            logger.LogInformation("Agent run processed locally. Prompt={PromptLen}chars", body.Prompt?.Length ?? 0);
            return Results.Ok(new AgentRunResponse
            {
                Narration = BuildNarration(body.Prompt ?? ""),
                Error = null,
                TransportSteps = [new TransportStepDto { Label = "Segurança", Detail = "OK", Ok = true }],
                ActiveStages = ["standalone"]
            });
        }).RequireRateLimiting("agent-run");

        app.MapGet("/policy/list", async (KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            var proxyResult = await kernel.ProxyGetAsync<object>("/policy/list", CancellationToken.None);
            if (proxyResult != null) return Results.Ok(proxyResult);

            logger.LogInformation("Policy list returned locally (no Kernel API)");
            return Results.Ok(new { policies = Array.Empty<object>(), totalCount = 0 });
        });

        app.MapGet("/agent/metrics/scorecard", async (KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            var proxyResult = await kernel.ProxyGetAsync<object>("/agent/metrics/scorecard", CancellationToken.None);
            if (proxyResult != null) return Results.Ok(proxyResult);

            logger.LogInformation("Scorecard returned locally (no Kernel API)");
            return Results.Ok(new { reliability = 0.0, efficiency = 0.0, safety = 0.0, antiLoop = 0.0, governance = 0.0, overall = 0.0, source = "local_fallback" });
        });

        app.MapPost("/memory/search", async (HttpContext ctx, KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            Dictionary<string, object>? body;
            try { body = await ctx.Request.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: ctx.RequestAborted); }
            catch { body = null; }
            if (body == null) return Results.BadRequest(new { error = "invalid_request" });
            if (body.Keys.Except(new[] { "query", "limit", "offset", "domain" }).Any())
                return Results.BadRequest(new { error = "unexpected_fields", allowed = new[] { "query", "limit", "offset", "domain" } });

            var proxyResult = await kernel.ProxyPostAsync<Dictionary<string, object>, object>("/memory/search", body, ctx.RequestAborted);
            if (proxyResult != null) return Results.Ok(proxyResult);

            logger.LogInformation("Memory search returned locally (no Kernel API)");
            return Results.Ok(new { hits = Array.Empty<object>(), totalCount = 0 });
        }).RequireRateLimiting("memory-read");

        app.MapGet("/memory/metrics", async (KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            var proxyResult = await kernel.ProxyGetAsync<object>("/memory/metrics", CancellationToken.None);
            if (proxyResult != null) return Results.Ok(proxyResult);

            logger.LogInformation("Memory metrics returned locally (no Kernel API)");
            return Results.Ok(new { totalChunks = 0, totalDocuments = 0, totalSizeBytes = 0 });
        }).RequireRateLimiting("memory-read");

        app.MapGet("/episodes/search", async (KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            var proxyResult = await kernel.ProxyGetAsync<object>("/episodes/search", CancellationToken.None);
            if (proxyResult != null) return Results.Ok(proxyResult);

            logger.LogInformation("Episodes search returned locally (no Kernel API)");
            return Results.Ok(new { episodes = Array.Empty<object>(), totalCount = 0 });
        });

        app.MapGet("/episodes/{id}", async (string id, KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            var proxyResult = await kernel.ProxyGetAsync<object>($"/episodes/{id}", CancellationToken.None);
            if (proxyResult != null) return Results.Ok(proxyResult);

            logger.LogInformation("Episode {Id} returned locally (no Kernel API)", id);
            return Results.Ok(new { id, goalId = "standalone", status = "idle", createdAt = DateTime.UtcNow });
        });

        return app;
    }

    private static string BuildNarration(string prompt) => prompt.ToLower() switch
    {
        var p when p.Contains("olá") || p.Contains("oi") => "Olá! Modo standalone local ativo. Como posso ajudar?",
        var p when p.Contains("ajuda") => "Modo standalone: segurança e risco ativos. Backend completo requer conexão remota.",
        var p when p.Contains("quem é") => "AI Kernel em modo standalone. Safety ativo.",
        _ => "Processado em modo standalone."
    };
}
