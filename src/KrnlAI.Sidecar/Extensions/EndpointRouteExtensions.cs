using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http.Headers;
using KrnlAI.Core.Abstractions.Safety;
using KrnlAI.Embedded.Abstractions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

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
        MapDiagnosticsEndpoints(app);

        if (hasPrefix)
        {
            MapHealthEndpoints(app, prefix);
            MapAgentRunEndpoint(app, prefix);
        }

        // Proxy-only endpoints (no v1 needed — they are thin wrappers)
        app.MapGet("/policy/list", async (HttpContext ctx, KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            var proxyResult = await kernel.ProxyGetAsync<object>("/policy/list", ctx.RequestAborted).ConfigureAwait(false);
            if (proxyResult != null) return Results.Ok(proxyResult);
            logger.LogInformation("Policy list returned locally (no KrnlAI API)");
            return Results.Ok(new { policies = Array.Empty<object>(), totalCount = 0 });
        });

        app.MapGet("/agent/metrics/scorecard", async (HttpContext ctx, KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            var proxyResult = await kernel.ProxyGetAsync<object>("/agent/metrics/scorecard", ctx.RequestAborted).ConfigureAwait(false);
            if (proxyResult != null) return Results.Ok(proxyResult);
            logger.LogInformation("Scorecard returned locally (no KrnlAI API)");
            return Results.Ok(new { reliability = 0.0, efficiency = 0.0, safety = 0.0, antiLoop = 0.0, governance = 0.0, overall = 0.0, source = "local_fallback" });
        });

        app.MapPost("/memory/search", async (HttpContext ctx, KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            Dictionary<string, object>? body;
            try { body = await ctx.Request.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: ctx.RequestAborted).ConfigureAwait(false); }
            catch { body = null; }
            var reqId = ctx.Items["RequestId"]?.ToString();
            if (body == null) return Results.BadRequest(new ErrorResponse("invalid_request", null, reqId));
            if (body.Keys.Except(["query", "limit", "topK", "offset", "domain"]).Any())
                return Results.BadRequest(new ErrorResponse("unexpected_fields", "Allowed: query, limit, topK, offset, domain", reqId));

            var proxyResult = await kernel.ProxyPostAsync<Dictionary<string, object>, object>("/memory/search", body, ctx.RequestAborted).ConfigureAwait(false);
            if (proxyResult != null) return Results.Ok(proxyResult);

            logger.LogInformation("Memory search returned locally (no KrnlAI API)");
            return Results.Ok(new { ok = true, hits = Array.Empty<object>(), totalCount = 0 });
        }).RequireRateLimiting("memory-read");

        app.MapGet("/memory/metrics", async (HttpContext ctx, KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            var proxyResult = await kernel.ProxyGetAsync<object>("/memory/metrics", ctx.RequestAborted).ConfigureAwait(false);
            if (proxyResult != null) return Results.Ok(proxyResult);

            logger.LogInformation("Memory metrics returned locally (no KrnlAI API)");
            return Results.Ok(new { totalChunks = 0, totalDocuments = 0, totalSizeBytes = 0 });
        }).RequireRateLimiting("memory-read");

        app.MapGet("/episodes/search", async (HttpContext ctx, KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            var proxyResult = await kernel.ProxyGetAsync<object>("/episodes/search", ctx.RequestAborted).ConfigureAwait(false);
            if (proxyResult != null) return Results.Ok(proxyResult);

            logger.LogInformation("Episodes search returned locally (no KrnlAI API)");
            return Results.Ok(new { episodes = Array.Empty<object>(), totalCount = 0 });
        });

        app.MapGet("/episodes/{id}", async (string id, HttpContext ctx, KernelApiProxy kernel, ILogger<Program> logger) =>
        {
            var proxyResult = await kernel.ProxyGetAsync<object>($"/episodes/{id}", ctx.RequestAborted).ConfigureAwait(false);
            if (proxyResult != null) return Results.Ok(proxyResult);

            logger.LogInformation("Episode {Id} returned locally (no KrnlAI API)", id);
            return Results.Ok(new { id, goalId = "standalone", status = "idle", createdAt = DateTime.UtcNow });
        });

        // Proxy-only endpoints (501 when no KrnlAI API)
        app.MapGet("/agent/status", async (HttpContext ctx, KernelApiProxy kernel) =>
        {
            var result = await kernel.ProxyGetAsync<object>("/agent/metrics/scorecard", ctx.RequestAborted).ConfigureAwait(false);
            return result is not null ? Results.Ok(result) : Results.Json(new ErrorResponse("not_implemented", "Available when KrnlAI API is configured", null), statusCode: 501);
        });

        app.MapGet("/goals/list", async (HttpContext ctx, KernelApiProxy kernel) =>
        {
            var result = await kernel.ProxyGetAsync<object>("/goals/active", ctx.RequestAborted).ConfigureAwait(false);
            return result is not null ? Results.Ok(result) : Results.Json(new ErrorResponse("not_implemented", "Available when KrnlAI API is configured", null), statusCode: 501);
        });

        app.MapGet("/goals/{id}", async (string id, HttpContext ctx, KernelApiProxy kernel) =>
        {
            var result = await kernel.ProxyGetAsync<object>($"/goals/{id}", ctx.RequestAborted).ConfigureAwait(false);
            return result is not null ? Results.Ok(result) : Results.Json(new ErrorResponse("not_implemented", "Available when KrnlAI API is configured", null), statusCode: 501);
        });

        app.MapGet("/emotions/current", async (HttpContext ctx, KernelApiProxy kernel) =>
        {
            var result = await kernel.ProxyGetAsync<object>("/profile/emotional", ctx.RequestAborted).ConfigureAwait(false);
            return result is not null ? Results.Ok(result) : Results.Json(new ErrorResponse("not_implemented", "Available when KrnlAI API is configured", null), statusCode: 501);
        });

        app.MapGet("/emotions/history", async (HttpContext ctx, KernelApiProxy kernel) =>
        {
            var result = await kernel.ProxyGetAsync<object>("/cognitive/affective-state", ctx.RequestAborted).ConfigureAwait(false);
            return result is not null ? Results.Ok(result) : Results.Json(new ErrorResponse("not_implemented", "Available when KrnlAI API is configured", null), statusCode: 501);
        });

        app.MapGet("/metacognition/status", async (HttpContext ctx, KernelApiProxy kernel) =>
        {
            var result = await kernel.ProxyGetAsync<object>("/cognitive/dashboard", ctx.RequestAborted).ConfigureAwait(false);
            return result is not null ? Results.Ok(result) : Results.Json(new ErrorResponse("not_implemented", "Available when KrnlAI API is configured", null), statusCode: 501);
        });

        // ── Profile / Emotional ──
        app.MapGet("/profile/emotional", (string? userId, ILogger<Program> logger) =>
        {
            logger.LogInformation("Profile emotional fallback (no KrnlAI API)");
            return Results.Ok(new { valence = 0.15, arousal = 0.3, motivation = 0.5, updatedAt = DateTime.UtcNow });
        });

        // ── Coding endpoints (local fallbacks, no proxy) ──
        app.MapPost("/api/coding/explain", (ILogger<Program> logger) =>
        {
            logger.LogInformation("Coding explain fallback (no KrnlAI API)");
            return Results.Ok(new { narration = "Funcionalidade de explicação disponível quando KrnlAI API estiver configurada.", command = (string?)null });
        });

        app.MapPost("/api/coding/fix", (ILogger<Program> logger) =>
        {
            logger.LogInformation("Coding fix fallback (no KrnlAI API)");
            return Results.Ok(new { narration = "Funcionalidade de correção disponível quando KrnlAI API estiver configurada.", command = (string?)null });
        });

        app.MapPost("/api/coding/test", (ILogger<Program> logger) =>
        {
            logger.LogInformation("Coding test fallback (no KrnlAI API)");
            return Results.Ok(new { narration = "Funcionalidade de geração de testes disponível quando KrnlAI API estiver configurada.", command = (string?)null });
        });

        app.MapPost("/api/coding/review", (ILogger<Program> logger) =>
        {
            logger.LogInformation("Coding review fallback (no KrnlAI API)");
            return Results.Ok(new { narration = "Funcionalidade de revisão disponível quando KrnlAI API estiver configurada.", command = (string?)null });
        });

        app.MapPost("/api/coding/suggest-fix", (ILogger<Program> logger) =>
        {
            logger.LogInformation("Suggest-fix fallback (no KrnlAI API)");
            return Results.Ok(new { title = "standalone", fix = (string?)null });
        });

        app.MapGet("/api/coding/approvals/pending", (ILogger<Program> logger) =>
        {
            logger.LogInformation("Approvals fallback (no KrnlAI API)");
            return Results.Ok(new { approvals = Array.Empty<object>() });
        });

        app.MapPost("/api/coding/approvals/{id}/respond", (string id, ILogger<Program> logger) =>
        {
            logger.LogInformation("Approval respond fallback (no KrnlAI API)");
            return Results.Ok(new { ok = true });
        });

        // ── Plugins ──
        app.MapGet("/admin/plugins/catalog", (ILogger<Program> logger) =>
        {
            logger.LogInformation("Plugin catalog fallback (no KrnlAI API)");
            return Results.Ok(new { items = Array.Empty<object>() });
        });

        app.MapPost("/admin/plugins/catalog/{pluginId}/install", (string pluginId, ILogger<Program> logger) =>
        {
            logger.LogInformation("Plugin install fallback (no KrnlAI API)");
            return Results.Ok(new { ok = false, error = "Plugin catalog requires KrnlAI API" });
        });

        // ── Diagnostics ──
        app.MapPost("/api/diagnostics/run", (ILogger<Program> logger) =>
        {
            logger.LogInformation("Diagnostics fallback (no KrnlAI API)");
            return Results.Ok(new
            {
                checks = new[]
                {
                    new { name = "sidecar_status", status = "passed", message = "Sidecar running" },
                    new { name = "krnlai_api", status = "skipped", message = "KrnlAI API not configured" },
                    new { name = "memory_store", status = "skipped", message = "No memory backend configured" },
                },
                systemInfo = new
                {
                    mode = "standalone",
                    os = Environment.OSVersion.ToString(),
                    dotnet = Environment.Version.ToString(),
                }
            });
        });

        // ── Backlog ──
        app.MapGet("/api/backlog", (string? status, ILogger<Program> logger) =>
        {
            logger.LogInformation("Backlog list fallback (no KrnlAI API)");
            return Results.Ok(Array.Empty<object>());
        });

        app.MapPost("/api/backlog", (ILogger<Program> logger) =>
        {
            logger.LogInformation("Backlog create fallback (no KrnlAI API)");
            return Results.Ok(new { id = Guid.NewGuid().ToString(), status = "standalone" });
        });

        app.MapGet("/api/backlog/{id}", (string id, ILogger<Program> logger) =>
        {
            logger.LogInformation("Backlog get fallback (no KrnlAI API)");
            return Results.Ok(new { id, title = "standalone", status = "pending" });
        });

        app.MapPatch("/api/backlog/{id}/status", (string id, ILogger<Program> logger) =>
        {
            logger.LogInformation("Backlog status update fallback (no KrnlAI API)");
            return Results.Ok(new { ok = true });
        });

        app.MapDelete("/api/backlog/{id}", (string id, ILogger<Program> logger) =>
        {
            logger.LogInformation("Backlog delete fallback (no KrnlAI API)");
            return Results.Ok(new { ok = true });
        });

        // ── QA ──
        app.MapPost("/api/qa/run", (ILogger<Program> logger) =>
        {
            logger.LogInformation("QA run fallback (no KrnlAI API)");
            return Results.Ok(new { id = Guid.NewGuid().ToString(), status = "standalone_fallback" });
        });

        app.MapGet("/api/qa/runs", (ILogger<Program> logger) =>
        {
            logger.LogInformation("QA runs list fallback (no KrnlAI API)");
            return Results.Ok(Array.Empty<object>());
        });

        app.MapGet("/api/qa/runs/{id}", (string id, ILogger<Program> logger) =>
        {
            logger.LogInformation("QA run get fallback (no KrnlAI API)");
            return Results.Ok(new { id, status = "standalone_fallback" });
        });

        app.MapGet("/api/qa/runs/{id}/evidence", (string id, ILogger<Program> logger) =>
        {
            logger.LogInformation("QA evidence fallback (no KrnlAI API)");
            return Results.Ok(Array.Empty<string>());
        });

        // ── Loop Execution ──
        app.MapPost("/api/loops/start", (ILogger<Program> logger) =>
        {
            logger.LogInformation("Loop start fallback (no KrnlAI API)");
            return Results.Ok(new { id = Guid.NewGuid().ToString(), status = "standalone_fallback" });
        });

        app.MapGet("/api/loops/{executionId}", (string executionId, ILogger<Program> logger) =>
        {
            logger.LogInformation("Loop status fallback (no KrnlAI API)");
            return Results.Ok(new { id = executionId, status = "standalone_fallback" });
        });

        app.MapGet("/api/loops/{itemId}/iterations", (string itemId, ILogger<Program> logger) =>
        {
            logger.LogInformation("Loop iterations fallback (no KrnlAI API)");
            return Results.Ok(Array.Empty<object>());
        });

        app.MapPost("/api/loops/{executionId}/cancel", (string executionId, ILogger<Program> logger) =>
        {
            logger.LogInformation("Loop cancel fallback (no KrnlAI API)");
            return Results.Ok(new { ok = true });
        });

        // ── Bridge Chat: LLM → KrnlAI AGI → LLM ──
        app.MapPost("/api/chat", async (ChatRequestDto request, IHttpClientFactory httpFactory, IEmbeddedKrnlAI? embeddedKernel, ILogger<Program> logger, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY")))
                return Results.Json(new { error = "DEEPSEEK_API_KEY not set" }, statusCode: 503);

            var baseUrl = Environment.GetEnvironmentVariable("DEEPSEEK_BASE_URL") ?? "https://api.deepseek.com";
            var model = Environment.GetEnvironmentVariable("DEEPSEEK_MODEL") ?? "deepseek-chat";
            var apiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");

            var baseUri = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";

            async Task<string> CallLlm(string system, string user, int maxTokens = 1024)
            {
                var c = httpFactory.CreateClient("deepseek-chat");
                c.BaseAddress = new Uri(baseUri);
                c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                var body = new Dictionary<string, object>
                {
                    ["model"] = model,
                    ["messages"] = new object[]
                    {
                        new { role = "system", content = system },
                        new { role = "user", content = user }
                    },
                    ["stream"] = false,
                    ["max_tokens"] = maxTokens,
                    ["temperature"] = 0.3
                };
                var resp = await c.PostAsJsonAsync("v1/chat/completions", body, ct).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();
                var json = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(ct).ConfigureAwait(false);
                if (json.TryGetProperty("choices", out var choices) && choices.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var choice in choices.EnumerateArray())
                    {
                        if (choice.TryGetProperty("message", out var msg) &&
                            msg.TryGetProperty("content", out var content) &&
                            content.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            return content.GetString() ?? "";
                        }
                    }
                }
                return "";
            }

            // Step 1: LLM traduz mensagem do usuário para goal do KrnlAI
            logger.LogInformation("Bridge Chat step 1: translating user text to AGI goal");
            var goal = (await CallLlm(
                "Você é um tradutor entre um usuário e um sistema AGI chamado KrnlAI. " +
                "Traduza a mensagem do usuário em um goal conciso e acionável para o AGI processar. " +
                "Responda APENAS com o goal, sem explicações, em português.",
                request.Prompt, 256)).Trim();

            if (string.IsNullOrWhiteSpace(goal))
                goal = request.Prompt;

            logger.LogInformation("Bridge Chat goal: {Goal}", goal);

            // Step 2: KrnlAI AGI processa o goal
            string narration;
            if (embeddedKernel != null)
            {
                var result = await embeddedKernel.RunAsync(goal, ct).ConfigureAwait(false);
                narration = result.Error ?? result.Narration;
                logger.LogInformation("Bridge Chat step 2: AGI processed, narLen={Len}", narration.Length);
            }
            else
            {
                narration = $"Processed goal: {goal}";
                logger.LogInformation("Bridge Chat step 2: no embedded kernel, using fallback");
            }

            // Step 3: LLM traduz a saída cognitiva para linguagem natural
            logger.LogInformation("Bridge Chat step 3: translating AGI output to natural language");
            var response = (await CallLlm(
                "Você é um narrador amigável que traduz a saída de um sistema AGI para linguagem natural. " +
                "O AGI processou internamente o pedido do usuário. Abaixo está o log cognitivo interno. " +
                "Explique o que aconteceu em linguagem natural, amigável e em português, " +
                "como se fosse um assistente inteligente conversando com o usuário. " +
                "NÃO mencione que você está traduzindo um log interno. Apenas responda naturalmente. " +
                "Se o AGI indicou erro ou bloqueio, informe o usuário educadamente.\n\n" +
                "Log cognitivo:\n" + narration,
                request.Prompt, request.MaxTokens > 0 ? request.MaxTokens : 2048)).Trim();

            logger.LogInformation("Bridge Chat step 3 complete, respLen={Len}", response.Length);
            return Results.Ok(new { response });
        });

        return app;
    }

    private static void MapDiagnosticsEndpoints(WebApplication app)
    {
        app.MapGet("/sidecar/diagnostics", (IOptions<SidecarOptions> opts) =>
        {
            var o = opts.Value;
            return Results.Ok(new
            {
                mode = o.EffectiveMode,
                auth = new
                {
                    token_configured = !string.IsNullOrWhiteSpace(o.Auth.Token),
                    api_key_configured = !string.IsNullOrWhiteSpace(o.Enterprise.ApiKey),
                    endpoint = o.Auth.Endpoint ?? o.Enterprise.AuthEndpoint ?? "local",
                },
                enterprise = o.Enterprise.Enabled ? new
                {
                    enabled = true,
                    gateway = o.Enterprise.GatewayEndpoint,
                    tenant = o.Enterprise.TenantId,
                } : null,
                kernel_api = new
                {
                    base_url = o.KernelApi.BaseUrl,
                    configured = !string.IsNullOrWhiteSpace(o.KernelApi.BaseUrl),
                },
            });
        });
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
                await ctx.Response.WriteAsync(json, cancellationToken: ctx.RequestAborted).ConfigureAwait(false);
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
            IFundamentalRulesEngine rules,
            IHybridSafetyEngine hybrid,
            IEthicalEnforcer ethics,
            ILawEnforcer law,
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
            try { body = await ctx.Request.ReadFromJsonAsync<AgentRunRequest>(cancellationToken: ct).ConfigureAwait(false); }
            catch { body = null; }
            var reqId = ctx.Items["RequestId"]?.ToString();
            if (body == null) return Results.BadRequest(new ErrorResponse("invalid_request", null, reqId));

            var prompt = body.Prompt ?? body.Goal ?? "";
            var transportSteps = new List<TransportStepDto>();

            // Layer 1: Adversarial input guard
            var safetyResult = await guard.ValidateAsync(prompt, ct).ConfigureAwait(false);
            if (!safetyResult.IsAllowed)
            {
                SafetyBlockedTotal.Add(1, new KeyValuePair<string, object?>("layer", "adversarial"));
                logger.LogWarning("Agent run blocked by AdversarialGuard. ThreatLevel={Threat}", safetyResult.ThreatLevel);
                return Results.Ok(new AgentRunTransportResponse(
                    Narration: "Conteudo bloqueado.",
                    Command: null,
                    TransportSteps: [new TransportStepDto("AdversarialGuard", "BLOQUEADO", false, null)],
                    ActiveStages: ["standalone"],
                    Error: "safety_block"
                ));
            }
            transportSteps.Add(new TransportStepDto("AdversarialGuard", "OK", true, null));

            // Layer 2: Fundamental Rules (R01-R21)
            var ruleResult = await rules.EvaluateAsync(prompt, "sidecar", ct).ConfigureAwait(false);
            if (!ruleResult.IsAllowed)
            {
                SafetyBlockedTotal.Add(1, new KeyValuePair<string, object?>("layer", "fundamental_rules"));
                logger.LogWarning("Agent run blocked by FundamentalRules. Violations={Violations}", string.Join(", ", ruleResult.ViolatedRules));
                return Results.Ok(new AgentRunTransportResponse(
                    Narration: "Acao bloqueada pelas regras fundamentais.",
                    Command: null,
                    TransportSteps: [.. transportSteps, new TransportStepDto("FundamentalRules", $"Violacoes: {ruleResult.ViolatedRules.Count}", false, null)],
                    ActiveStages: ["standalone"],
                    Error: "rules_block"
                ));
            }
            if (ruleResult.ViolatedRules.Any())
                transportSteps.Add(new TransportStepDto("FundamentalRules", $"Avisos: {ruleResult.ViolatedRules.Count}", true, null));
            else
                transportSteps.Add(new TransportStepDto("FundamentalRules", "OK", true, null));

            // Layer 3: Hybrid safety (contextual + semantic)
            var hybridResult = await hybrid.EvaluateAsync(prompt, "sidecar", ct).ConfigureAwait(false);
            if (!hybridResult.IsAllowed)
            {
                SafetyBlockedTotal.Add(1, new KeyValuePair<string, object?>("layer", "hybrid"));
                logger.LogWarning("Agent run blocked by HybridSafetyEngine. Violations={Violations}", string.Join(", ", hybridResult.ViolatedRules));
                return Results.Ok(new AgentRunTransportResponse(
                    Narration: "Acao bloqueada pela avaliacao hibrida de seguranca.",
                    Command: null,
                    TransportSteps: [.. transportSteps, new TransportStepDto("HybridSafety", $"Violacoes: {hybridResult.ViolatedRules.Count}", false, null)],
                    ActiveStages: ["standalone"],
                    Error: "hybrid_safety_block"
                ));
            }
            transportSteps.Add(new TransportStepDto("HybridSafety", "OK", true, null));

            // Layer 4: Ethical enforcement
            var ethical = ethics.Assess(prompt);
            if (!ethical.Approved)
            {
                SafetyBlockedTotal.Add(1, new KeyValuePair<string, object?>("layer", "ethical"));
                logger.LogWarning("Agent run blocked by EthicalEnforcer. Reasons={Reasons}", string.Join(", ", ethical.PrinciplesViolated));
                return Results.Ok(new AgentRunTransportResponse(
                    Narration: "Acao bloqueada pela avaliacao etica.",
                    Command: null,
                    TransportSteps: [.. transportSteps, new TransportStepDto("EthicalEnforcer", $"Violacoes: {ethical.PrinciplesViolated.Count}", false, null)],
                    ActiveStages: ["standalone"],
                    Error: "ethical_block"
                ));
            }
            transportSteps.Add(new TransportStepDto("EthicalEnforcer", "OK", true, null));

            // Layer 5: Law enforcement
            var lawAction = new ProposedAction("agent_run", new Dictionary<string, string> { ["prompt"] = prompt }, prompt);
            var lawCtx = new KrnlAI.Contracts.Safety.ExecutionContext(new WorldStateSnapshot([]), [], false, null, 0, [], "General");
            var lawResult = law.Evaluate(lawAction, lawCtx);
            if (!lawResult.IsAllowed)
            {
                SafetyBlockedTotal.Add(1, new KeyValuePair<string, object?>("layer", "law"));
                logger.LogWarning("Agent run blocked by LawEnforcer. Law={Law}, Reason={Reason}", lawResult.BlockedByLaw, lawResult.BlockReason);
                return Results.Ok(new AgentRunTransportResponse(
                    Narration: "Acao bloqueada pela avaliacao legal.",
                    Command: null,
                    TransportSteps: [.. transportSteps, new TransportStepDto("LawEnforcer", $"{lawResult.BlockedByLaw}: {lawResult.BlockReason}", false, null)],
                    ActiveStages: ["standalone"],
                    Error: "law_block"
                ));
            }
            transportSteps.Add(new TransportStepDto("LawEnforcer", "OK", true, null));

            SafetyPassedTotal.Add(1);

            // Try proxy to KrnlAI API first
            var proxyResult = await kernel.ProxyPostAsync<AgentRunRequest, AgentRunTransportResponse>("/v1/agent/run", body, ct).ConfigureAwait(false);
            if (proxyResult != null)
            {
                sw.Stop();
                HttpDuration.Record(sw.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("endpoint", "agent_run"), new KeyValuePair<string, object?>("status", "proxy"));
                return Results.Ok(new AgentRunTransportResponse(
                    Narration: proxyResult.Narration,
                    Command: proxyResult.Command,
                    TransportSteps: [.. transportSteps, .. proxyResult.TransportSteps ?? []],
                    ActiveStages: proxyResult.ActiveStages,
                    Error: proxyResult.Error
                ));
            }

            // Fallback: try EmbeddedKrnlAI locally
            if (embeddedKernel != null)
            {
                logger.LogInformation("Agent run using EmbeddedKrnlAI fallback. Prompt={PromptLen}chars", prompt.Length);
                var embeddedResult = await embeddedKernel.RunAsync(prompt, ct).ConfigureAwait(false);
                sw.Stop();
                HttpDuration.Record(sw.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("endpoint", "agent_run"), new KeyValuePair<string, object?>("status", "embedded"));
                return Results.Ok(new AgentRunTransportResponse(
                    Narration: embeddedResult.Narration,
                    Command: null,
                    TransportSteps: [.. transportSteps, new TransportStepDto("EmbeddedKrnlAI", embeddedResult.Mode, embeddedResult.Error is null, null)],
                    ActiveStages: ["standalone"],
                    Error: embeddedResult.Error
                ));
            }

            logger.LogInformation("Agent run with no fallback available. Prompt={PromptLen}chars", prompt.Length);
            sw.Stop();
            HttpDuration.Record(sw.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("endpoint", "agent_run"), new KeyValuePair<string, object?>("status", "unavailable"));
            return Results.Ok(new AgentRunTransportResponse(
                Narration: "Krnl-AI em modo standalone. Safety ativo.",
                Command: null,
                TransportSteps: [.. transportSteps, new TransportStepDto("Narration", "no_fallback", true, null)],
                ActiveStages: ["standalone"],
                Error: null
            ));
        }).RequireRateLimiting("agent-run");
    }
}

public record ChatRequestDto(string Prompt, string? SystemPrompt = null, double? Temperature = null, int MaxTokens = 0);
