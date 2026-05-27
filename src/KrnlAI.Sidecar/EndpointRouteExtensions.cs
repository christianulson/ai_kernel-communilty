using System.Diagnostics;
using System.Diagnostics.Metrics;
using KrnlAI.Core.Abstractions;
using KrnlAI.Core.Services.Safety;
using KrnlAI.Embedded.Abstractions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SafetyExecutionContext = KrnlAI.Core.Services.Safety.ExecutionContext;

namespace KrnlAI.Sidecar;

public static class EndpointRouteExtensions
{
    private static readonly Meter Meter = new("KrnlAI.Sidecar");
    private static readonly Counter<int> SafetyBlockedTotal = Meter.CreateCounter<int>("sidecar_safety_blocked_total", description: "Total blocked by safety layer");
    private static readonly Counter<int> SafetyPassedTotal = Meter.CreateCounter<int>("sidecar_safety_passed_total", description: "Total passed all safety layers");
    private static readonly Histogram<double> HttpDuration = Meter.CreateHistogram<double>("sidecar_http_duration_seconds", description: "HTTP duration in seconds");

    public static WebApplication MapSidecarEndpoints(this WebApplication app)
    {
        var prefix = app.Services.GetRequiredService<IOptions<SidecarOptions>>().Value.ApiVersion.Prefix;
        var hasPrefix = !string.IsNullOrWhiteSpace(prefix);

        MapHealthEndpoints(app, "");
        MapAgentRunEndpoint(app, "");

        if (hasPrefix)
        {
            MapHealthEndpoints(app, prefix);
            MapAgentRunEndpoint(app, prefix);
        }

        // Proxy-only endpoints (no v1 needed — they are thin wrappers)
        app.MapGet("/policy/list", async (HttpContext ctx, KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            var proxyResult = await kernel.ProxyGetAsync<object>("/policy/list", ctx.RequestAborted);
            if (proxyResult != null) return Results.Ok(proxyResult);
            logger.LogInformation("Policy list returned locally (no KrnlAI API)");
            return Results.Ok(new { policies = Array.Empty<object>(), totalCount = 0 });
        });

        app.MapGet("/agent/metrics/scorecard", async (HttpContext ctx, KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            var proxyResult = await kernel.ProxyGetAsync<object>("/agent/metrics/scorecard", ctx.RequestAborted);
            if (proxyResult != null) return Results.Ok(proxyResult);
            logger.LogInformation("Scorecard returned locally (no KrnlAI API)");
            return Results.Ok(new { reliability = 0.0, efficiency = 0.0, safety = 0.0, antiLoop = 0.0, governance = 0.0, overall = 0.0, source = "local_fallback" });
        });

        app.MapPost("/memory/search", async (HttpContext ctx, KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            Dictionary<string, object>? body;
            try { body = await ctx.Request.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: ctx.RequestAborted); }
            catch { body = null; }
            var reqId = ctx.Items["RequestId"]?.ToString();
            if (body == null) return Results.BadRequest(new ErrorResponse("invalid_request", null, reqId));
            if (body.Keys.Except(new[] { "query", "limit", "topK", "offset", "domain" }).Any())
                return Results.BadRequest(new ErrorResponse("unexpected_fields", "Allowed: query, limit, topK, offset, domain", reqId));

            var proxyResult = await kernel.ProxyPostAsync<Dictionary<string, object>, object>("/memory/search", body, ctx.RequestAborted);
            if (proxyResult != null) return Results.Ok(proxyResult);

            logger.LogInformation("Memory search returned locally (no KrnlAI API)");
            return Results.Ok(new { ok = true, hits = Array.Empty<object>(), totalCount = 0 });
        }).RequireRateLimiting("memory-read");

        app.MapGet("/memory/metrics", async (HttpContext ctx, KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            var proxyResult = await kernel.ProxyGetAsync<object>("/memory/metrics", ctx.RequestAborted);
            if (proxyResult != null) return Results.Ok(proxyResult);

            logger.LogInformation("Memory metrics returned locally (no KrnlAI API)");
            return Results.Ok(new { totalChunks = 0, totalDocuments = 0, totalSizeBytes = 0 });
        }).RequireRateLimiting("memory-read");

        app.MapGet("/episodes/search", async (HttpContext ctx, KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            var proxyResult = await kernel.ProxyGetAsync<object>("/episodes/search", ctx.RequestAborted);
            if (proxyResult != null) return Results.Ok(proxyResult);

            logger.LogInformation("Episodes search returned locally (no KrnlAI API)");
            return Results.Ok(new { episodes = Array.Empty<object>(), totalCount = 0 });
        });

        app.MapGet("/episodes/{id}", async (string id, HttpContext ctx, KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            var proxyResult = await kernel.ProxyGetAsync<object>($"/episodes/{id}", ctx.RequestAborted);
            if (proxyResult != null) return Results.Ok(proxyResult);

            logger.LogInformation("Episode {Id} returned locally (no KrnlAI API)", id);
            return Results.Ok(new { id, goalId = "standalone", status = "idle", createdAt = DateTime.UtcNow });
        });

        // Proxy-only endpoints (501 when no KrnlAI API)
        app.MapGet("/agent/status", async (HttpContext ctx, KernelApiProxy kernel) =>
        {
            var result = await kernel.ProxyGetAsync<object>("/agent/metrics/scorecard", ctx.RequestAborted);
            return result is not null ? Results.Ok(result) : Results.Json(new ErrorResponse("not_implemented", "Available when KrnlAI API is configured", null), statusCode: 501);
        });

        app.MapGet("/goals/list", async (HttpContext ctx, KernelApiProxy kernel) =>
        {
            var result = await kernel.ProxyGetAsync<object>("/goals/active", ctx.RequestAborted);
            return result is not null ? Results.Ok(result) : Results.Json(new ErrorResponse("not_implemented", "Available when KrnlAI API is configured", null), statusCode: 501);
        });

        app.MapGet("/goals/{id}", async (string id, HttpContext ctx, KernelApiProxy kernel) =>
        {
            var result = await kernel.ProxyGetAsync<object>($"/goals/{id}", ctx.RequestAborted);
            return result is not null ? Results.Ok(result) : Results.Json(new ErrorResponse("not_implemented", "Available when KrnlAI API is configured", null), statusCode: 501);
        });

        app.MapGet("/emotions/current", async (HttpContext ctx, KernelApiProxy kernel) =>
        {
            var result = await kernel.ProxyGetAsync<object>("/profile/emotional", ctx.RequestAborted);
            return result is not null ? Results.Ok(result) : Results.Json(new ErrorResponse("not_implemented", "Available when KrnlAI API is configured", null), statusCode: 501);
        });

        app.MapGet("/emotions/history", async (HttpContext ctx, KernelApiProxy kernel) =>
        {
            var result = await kernel.ProxyGetAsync<object>("/cognitive/affective-state", ctx.RequestAborted);
            return result is not null ? Results.Ok(result) : Results.Json(new ErrorResponse("not_implemented", "Available when KrnlAI API is configured", null), statusCode: 501);
        });

        app.MapGet("/metacognition/status", async (HttpContext ctx, KernelApiProxy kernel) =>
        {
            var result = await kernel.ProxyGetAsync<object>("/cognitive/dashboard", ctx.RequestAborted);
            return result is not null ? Results.Ok(result) : Results.Json(new ErrorResponse("not_implemented", "Available when KrnlAI API is configured", null), statusCode: 501);
        });

        return app;
    }

    private static void MapHealthEndpoints(WebApplication app, string prefix)
    {
        var hp = prefix + "/health";
        app.MapHealthChecks(hp, new HealthCheckOptions
        {
            ResponseWriter = async (ctx, report) =>
            {
                ctx.Response.ContentType = "application/json";
                var json = System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    ts = DateTime.UtcNow,
                    version = "KrnlAI.Sidecar/1.0.0",
                    checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() })
                });
                await ctx.Response.WriteAsync(json, cancellationToken: ctx.RequestAborted);
            }
        }).RequireRateLimiting("health");

        app.MapHealthChecks(prefix + "/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") })
            .RequireRateLimiting("health");
        app.MapHealthChecks(prefix + "/health/live", new HealthCheckOptions { Predicate = check => check.Tags.Contains("live") })
            .RequireRateLimiting("health");
    }

    private static void MapAgentRunEndpoint(WebApplication app, string prefix)
    {
        app.MapPost(prefix + "/agent/run", async (
            HttpContext ctx,
            IAdversarialGuard guard,
            FundamentalRulesEngine rules,
            HybridSafetyEngine hybrid,
            EthicalEnforcer ethics,
            LawEnforcer law,
            KernelApiProxy kernel,
            IEmbeddedKrnlAI? embeddedKernel,
            IOptions<SidecarOptions> options,
            ILogger<Program> logger) =>
        {
            var timeoutSec = options.Value.AgentRun.TimeoutSeconds;
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ctx.RequestAborted);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, timeoutSec)));
            var ct = timeoutCts.Token;

            var sw = Stopwatch.StartNew();
            AgentRunRequest? body;
            try { body = await ctx.Request.ReadFromJsonAsync<AgentRunRequest>(cancellationToken: ct); }
            catch { body = null; }
            var reqId = ctx.Items["RequestId"]?.ToString();
            if (body == null) return Results.BadRequest(new ErrorResponse("invalid_request", null, reqId));

            var prompt = body.Prompt ?? body.Goal ?? "";
            var transportSteps = new List<TransportStepDto>();

            // Layer 1: Adversarial input guard
            var safetyResult = await guard.ValidateAsync(prompt, ct);
            if (!safetyResult.IsAllowed)
            {
                SafetyBlockedTotal.Add(1, new KeyValuePair<string, object?>("layer", "adversarial"));
                logger.LogWarning("Agent run blocked by AdversarialGuard. ThreatLevel={Threat}", safetyResult.ThreatLevel);
                return Results.Ok(new AgentRunResponse
                {
                    Narration = "Conteudo bloqueado.",
                    Error = "safety_block",
                    TransportSteps = [new TransportStepDto { Label = "AdversarialGuard", Detail = "BLOQUEADO", Ok = false }],
                    ActiveStages = ["standalone"]
                });
            }
            transportSteps.Add(new TransportStepDto { Label = "AdversarialGuard", Detail = "OK", Ok = true });

            // Layer 2: Fundamental Rules (R01-R21)
            var ruleResult = await rules.EvaluateAsync(prompt, "sidecar", ct);
            if (!ruleResult.IsAllowed)
            {
                SafetyBlockedTotal.Add(1, new KeyValuePair<string, object?>("layer", "fundamental_rules"));
                logger.LogWarning("Agent run blocked by FundamentalRules. Violations={Violations}", string.Join(", ", ruleResult.ViolatedRules));
                return Results.Ok(new AgentRunResponse
                {
                    Narration = "Acao bloqueada pelas regras fundamentais.",
                    Error = "rules_block",
                    TransportSteps = [.. transportSteps, new TransportStepDto { Label = "FundamentalRules", Detail = $"Violacoes: {ruleResult.ViolatedRules.Count}", Ok = false }],
                    ActiveStages = ["standalone"]
                });
            }
            if (ruleResult.ViolatedRules.Any())
                transportSteps.Add(new TransportStepDto { Label = "FundamentalRules", Detail = $"Avisos: {ruleResult.ViolatedRules.Count}", Ok = true });
            else
                transportSteps.Add(new TransportStepDto { Label = "FundamentalRules", Detail = "OK", Ok = true });

            // Layer 3: Hybrid safety (contextual + semantic)
            var hybridResult = await hybrid.EvaluateAsync(prompt, "sidecar", ct);
            if (!hybridResult.IsAllowed)
            {
                SafetyBlockedTotal.Add(1, new KeyValuePair<string, object?>("layer", "hybrid"));
                logger.LogWarning("Agent run blocked by HybridSafetyEngine. Violations={Violations}", string.Join(", ", hybridResult.ViolatedRules));
                return Results.Ok(new AgentRunResponse
                {
                    Narration = "Acao bloqueada pela avaliacao hibrida de seguranca.",
                    Error = "hybrid_safety_block",
                    TransportSteps = [.. transportSteps, new TransportStepDto { Label = "HybridSafety", Detail = $"Violacoes: {hybridResult.ViolatedRules.Count}", Ok = false }],
                    ActiveStages = ["standalone"]
                });
            }
            transportSteps.Add(new TransportStepDto { Label = "HybridSafety", Detail = "OK", Ok = true });

            // Layer 4: Ethical enforcement
            var ethical = ethics.Assess(prompt);
            if (!ethical.Approved)
            {
                SafetyBlockedTotal.Add(1, new KeyValuePair<string, object?>("layer", "ethical"));
                logger.LogWarning("Agent run blocked by EthicalEnforcer. Reasons={Reasons}", string.Join(", ", ethical.PrinciplesViolated));
                return Results.Ok(new AgentRunResponse
                {
                    Narration = "Acao bloqueada pela avaliacao etica.",
                    Error = "ethical_block",
                    TransportSteps = [.. transportSteps, new TransportStepDto { Label = "EthicalEnforcer", Detail = $"Violacoes: {ethical.PrinciplesViolated.Count}", Ok = false }],
                    ActiveStages = ["standalone"]
                });
            }
            transportSteps.Add(new TransportStepDto { Label = "EthicalEnforcer", Detail = "OK", Ok = true });

            // Layer 5: Law enforcement
            var lawAction = new ProposedAction("agent_run", new Dictionary<string, string> { ["prompt"] = prompt }, prompt);
            var lawCtx = new SafetyExecutionContext(new WorldStateSnapshot([]), [], false, null, 0, [], "General");
            var lawResult = law.Evaluate(lawAction, lawCtx);
            if (!lawResult.IsAllowed)
            {
                SafetyBlockedTotal.Add(1, new KeyValuePair<string, object?>("layer", "law"));
                logger.LogWarning("Agent run blocked by LawEnforcer. Law={Law}, Reason={Reason}", lawResult.BlockedByLaw, lawResult.BlockReason);
                return Results.Ok(new AgentRunResponse
                {
                    Narration = "Acao bloqueada pela avaliacao legal.",
                    Error = "law_block",
                    TransportSteps = [.. transportSteps, new TransportStepDto { Label = "LawEnforcer", Detail = $"{lawResult.BlockedByLaw}: {lawResult.BlockReason}", Ok = false }],
                    ActiveStages = ["standalone"]
                });
            }
            transportSteps.Add(new TransportStepDto { Label = "LawEnforcer", Detail = "OK", Ok = true });

            SafetyPassedTotal.Add(1);

            // Try proxy to KrnlAI API first
            var proxyResult = await kernel.ProxyPostAsync<AgentRunRequest, AgentRunResponse>("/v1/agent/run", body, ct);
            if (proxyResult != null)
            {
                sw.Stop();
                HttpDuration.Record(sw.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("endpoint", "agent_run"), new KeyValuePair<string, object?>("status", "proxy"));
                return Results.Ok(new AgentRunResponse
                {
                    Narration = proxyResult.Narration,
                    Error = proxyResult.Error,
                    TransportSteps = [.. transportSteps, .. proxyResult.TransportSteps ?? []],
                    ActiveStages = proxyResult.ActiveStages
                });
            }

            // Fallback: try EmbeddedKrnlAI locally
            if (embeddedKernel != null)
            {
                logger.LogInformation("Agent run using EmbeddedKrnlAI fallback. Prompt={PromptLen}chars", prompt.Length);
                var embeddedResult = await embeddedKernel.RunAsync(prompt, ct);
                sw.Stop();
                HttpDuration.Record(sw.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("endpoint", "agent_run"), new KeyValuePair<string, object?>("status", "embedded"));
                return Results.Ok(new AgentRunResponse
                {
                    Narration = embeddedResult.Narration,
                    Error = embeddedResult.Error,
                    TransportSteps = [.. transportSteps, new TransportStepDto { Label = "EmbeddedKrnlAI", Detail = embeddedResult.Mode, Ok = embeddedResult.Error is null }],
                    ActiveStages = ["standalone"]
                });
            }

            logger.LogInformation("Agent run with no fallback available. Prompt={PromptLen}chars", prompt.Length);
            sw.Stop();
            HttpDuration.Record(sw.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("endpoint", "agent_run"), new KeyValuePair<string, object?>("status", "unavailable"));
            return Results.Ok(new AgentRunResponse
            {
                Narration = "Krnl-AI em modo standalone. Safety ativo.",
                Error = null,
                TransportSteps = [.. transportSteps, new TransportStepDto { Label = "Narration", Detail = "no_fallback", Ok = true }],
                ActiveStages = ["standalone"]
            });
        }).RequireRateLimiting("agent-run");
    }
}
