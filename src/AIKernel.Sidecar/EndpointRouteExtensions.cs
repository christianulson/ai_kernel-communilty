using Kernel.Abstractions;
using Kernel.Core.Services.Safety;
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

        app.MapPost("/agent/run", async (HttpContext ctx, IAdversarialGuard guard, FundamentalRulesEngine rules, EthicalEnforcer ethics, KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            AgentRunRequest? body;
            try { body = await ctx.Request.ReadFromJsonAsync<AgentRunRequest>(cancellationToken: ctx.RequestAborted); }
            catch { body = null; }
            if (body == null) return Results.BadRequest(new { error = "invalid_request" });

            var prompt = body.Prompt ?? "";
            var transportSteps = new List<TransportStepDto>();

            // Layer 1: Adversarial input guard
            var safetyResult = await guard.ValidateAsync(prompt, ctx.RequestAborted);
            if (!safetyResult.IsAllowed)
            {
                logger.LogWarning("Agent run blocked by AdversarialGuard. ThreatLevel={Threat}", safetyResult.ThreatLevel);
                return Results.Ok(new AgentRunResponse
                {
                    Narration = "Conteúdo bloqueado.",
                    Error = "safety_block",
                    TransportSteps = [new TransportStepDto { Label = "AdversarialGuard", Detail = "BLOQUEADO", Ok = false }],
                    ActiveStages = ["standalone"]
                });
            }
            transportSteps.Add(new TransportStepDto { Label = "AdversarialGuard", Detail = "OK", Ok = true });

            // Layer 2: Fundamental Rules (R01-R20)
            var ruleResult = await rules.EvaluateAsync(prompt, "sidecar", ctx.RequestAborted);
            if (!ruleResult.IsAllowed)
            {
                logger.LogWarning("Agent run blocked by FundamentalRules. Violations={Violations}", string.Join(", ", ruleResult.ViolatedRules));
                return Results.Ok(new AgentRunResponse
                {
                    Narration = "Ação bloqueada pelas regras fundamentais.",
                    Error = "rules_block",
                    TransportSteps = [.. transportSteps, new TransportStepDto { Label = "FundamentalRules", Detail = $"Violações: {ruleResult.ViolatedRules.Count}", Ok = false }],
                    ActiveStages = ["standalone"]
                });
            }
            if (ruleResult.ViolatedRules.Any())
                transportSteps.Add(new TransportStepDto { Label = "FundamentalRules", Detail = $"Avisos: {ruleResult.ViolatedRules.Count}", Ok = true });
            else
                transportSteps.Add(new TransportStepDto { Label = "FundamentalRules", Detail = "OK", Ok = true });

            // Layer 3: Ethical enforcement
            var ethical = ethics.Assess(prompt);
            if (!ethical.Approved)
            {
                logger.LogWarning("Agent run blocked by EthicalEnforcer. Reasons={Reasons}", string.Join(", ", ethical.PrinciplesViolated));
                return Results.Ok(new AgentRunResponse
                {
                    Narration = "Ação bloqueada pela avaliação ética.",
                    Error = "ethical_block",
                    TransportSteps = [.. transportSteps, new TransportStepDto { Label = "EthicalEnforcer", Detail = $"Violações: {ethical.PrinciplesViolated.Count}", Ok = false }],
                    ActiveStages = ["standalone"]
                });
            }
            transportSteps.Add(new TransportStepDto { Label = "EthicalEnforcer", Detail = "OK", Ok = true });

            // Try proxy to Kernel API first
            var proxyResult = await kernel.ProxyPostAsync<AgentRunRequest, AgentRunResponse>("/v1/agent/run", body, ctx.RequestAborted);
            if (proxyResult != null)
            {
                return Results.Ok(new AgentRunResponse
                {
                    Narration = proxyResult.Narration,
                    Error = proxyResult.Error,
                    TransportSteps = [.. transportSteps, .. proxyResult.TransportSteps ?? []],
                    ActiveStages = proxyResult.ActiveStages
                });
            }

            logger.LogInformation("Agent run processed locally. Prompt={PromptLen}chars", prompt.Length);
            return Results.Ok(new AgentRunResponse
            {
                Narration = BuildNarration(prompt),
                Error = null,
                TransportSteps = [.. transportSteps, new TransportStepDto { Label = "Narration", Detail = "local_fallback", Ok = true }],
                ActiveStages = ["standalone"]
            });
        }).RequireRateLimiting("agent-run");

        app.MapGet("/policy/list", async (HttpContext ctx, KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            var proxyResult = await kernel.ProxyGetAsync<object>("/policy/list", ctx.RequestAborted);
            if (proxyResult != null) return Results.Ok(proxyResult);

            logger.LogInformation("Policy list returned locally (no Kernel API)");
            return Results.Ok(new { policies = Array.Empty<object>(), totalCount = 0 });
        });

        app.MapGet("/agent/metrics/scorecard", async (HttpContext ctx, KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            var proxyResult = await kernel.ProxyGetAsync<object>("/agent/metrics/scorecard", ctx.RequestAborted);
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

        app.MapGet("/episodes/search", async (HttpContext ctx, KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            var proxyResult = await kernel.ProxyGetAsync<object>("/episodes/search", ctx.RequestAborted);
            if (proxyResult != null) return Results.Ok(proxyResult);

            logger.LogInformation("Episodes search returned locally (no Kernel API)");
            return Results.Ok(new { episodes = Array.Empty<object>(), totalCount = 0 });
        });

        app.MapGet("/episodes/{id}", async (string id, HttpContext ctx, KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            var proxyResult = await kernel.ProxyGetAsync<object>($"/episodes/{id}", ctx.RequestAborted);
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
