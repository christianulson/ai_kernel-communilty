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

        app.MapPost("/agent/run", async (HttpContext ctx, IAdversarialGuard guard, ILogger<Program> logger) =>
        {
            AgentRunRequest? body;
            try
            {
                body = await ctx.Request.ReadFromJsonAsync<AgentRunRequest>(cancellationToken: ctx.RequestAborted);
            }
            catch
            {
                body = null;
            }
            if (body == null) return Results.BadRequest(new { error = "invalid_request" });
            var safetyResult = await guard.ValidateAsync(body.Prompt ?? "", ctx.RequestAborted);
            var blocked = !safetyResult.IsAllowed;
            if (blocked)
                logger.LogWarning("Agent run blocked. ThreatLevel={Threat}, Patterns={Patterns}", safetyResult.ThreatLevel, safetyResult.DetectedPatterns);
            else
                logger.LogInformation("Agent run allowed. Prompt={PromptLen}chars", body.Prompt?.Length ?? 0);
            return Results.Ok(new AgentRunResponse
            {
                Narration = blocked ? "Conteúdo bloqueado." : BuildNarration(body.Prompt ?? ""),
                Error = blocked ? "safety_block" : null,
                TransportSteps = new[] { new TransportStepDto { Label = "Segurança", Detail = blocked ? "BLOQUEADO" : "OK", Ok = !blocked } },
                ActiveStages = new[] { "standalone" }
            });
        }).RequireRateLimiting("agent-run");

        app.MapGet("/policy/list", (string? domain) =>
            Results.Ok(new { policies = new[] { new { id = "p1", name = "Default Policy", domain = "general", version = "1.0", createdAt = DateTime.UtcNow, isActive = true } }, totalCount = 1 }));

        app.MapGet("/agent/metrics/scorecard", () => Results.Ok(new { reliability = 0.85, efficiency = 0.78, safety = 0.95, antiLoop = 0.88, governance = 0.82, overall = 0.86 }));

        app.MapPost("/memory/search", async (HttpContext ctx, ILogger<Program> logger) =>
        {
            Dictionary<string, object>? body;
            try
            {
                body = await ctx.Request.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: ctx.RequestAborted);
            }
            catch
            {
                body = null;
            }
            if (body == null) return Results.BadRequest(new { error = "invalid_request" });
            if (body.Keys.Except(new[] { "query", "limit", "offset", "domain" }).Any())
                return Results.BadRequest(new { error = "unexpected_fields", allowed = new[] { "query", "limit", "offset", "domain" } });
            logger.LogInformation("Memory search requested with {KeyCount} keys", body.Count);
            return Results.Ok(new { hits = Array.Empty<object>(), totalCount = 0 });
        }).RequireRateLimiting("memory-read");

        app.MapGet("/memory/metrics", () => Results.Ok(new { totalChunks = 0, totalDocuments = 0, totalSizeBytes = 0 }))
            .RequireRateLimiting("memory-read");

        app.MapGet("/episodes/search", () => Results.Ok(new { episodes = Array.Empty<object>(), totalCount = 0 }));
        app.MapGet("/episodes/{id}", (string id) => Results.Ok(new { id, goalId = "standalone", status = "idle", createdAt = DateTime.UtcNow }));

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
