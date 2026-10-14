using KrnlAI.Embedded.Abstractions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace KrnlAI.Sidecar;

public static class CommunityEndpoints
{
    public static WebApplication MapCommunityEndpoints(this WebApplication app)
    {
        app.ConfigureSidecarPipeline();

        app.MapPost("/agent/run", async (HttpContext ctx, AgentRunRequest request, IEmbeddedKrnlAI kernel, ILogger<Program> logger, CancellationToken ct) =>
        {
            var prompt = request.Prompt ?? request.Goal ?? string.Empty;
            logger.LogInformation("Community agent/run: prompt={PromptLen}chars, ip={RemoteIp}", prompt.Length, ctx.Connection.RemoteIpAddress);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = await kernel.RunAsync(prompt, ct).ConfigureAwait(false);
            sw.Stop();
            logger.LogInformation("Community agent/run completed: duration={Duration}ms, error={Error}", sw.ElapsedMilliseconds, result.Error);
            return Results.Ok(new AgentRunTransportResponse(
                Narration: result.Narration,
                Command: null,
                TransportSteps: [new TransportStepDto("EmbeddedKrnlAI", result.Mode, result.Error is null, null)],
                ActiveStages: ["community"],
                Error: result.Error
            ));
        }).RequireRateLimiting("agent-run");

        app.MapPost("/memory/search", async (HttpContext ctx, Dictionary<string, object>? body, IEmbeddedKrnlAI kernel, ILogger<Program> logger, CancellationToken ct) =>
        {
            if (body is null)
                return Results.BadRequest(new { error = "invalid_request" });

            var allowed = new[] { "query", "limit", "topK", "offset", "domain" };
            if (body.Keys.Except(allowed).Any())
                return Results.BadRequest(new { error = "unexpected_fields", allowed });

            var queryStr = body.TryGetValue("query", out var q) ? q?.ToString() : null;
            if (string.IsNullOrWhiteSpace(queryStr))
                return Results.BadRequest(new { error = "query_required" });

            logger.LogInformation("Community memory/search: query={Query}, ip={RemoteIp}", queryStr, ctx.Connection.RemoteIpAddress);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var hits = await kernel.SearchMemoryAsync(queryStr, ct).ConfigureAwait(false);
            sw.Stop();
            logger.LogInformation("Community memory/search completed: hits={HitCount}, duration={Duration}ms", hits.Count, sw.ElapsedMilliseconds);
            var mappedHits = hits.Select(h => new
            {
                id = h.Id,
                content = h.Payload ?? "",
                source = "embedded",
                score = h.Score,
                docId = h.Id,
                chunkId = h.Id,
                text = h.Payload ?? ""
            }).ToArray();
            return Results.Ok(new { ok = true, hits = mappedHits, totalCount = mappedHits.Length, queryTimeMs = sw.Elapsed.TotalMilliseconds, mode = "community" });
        });

        app.MapGet("/policy/list", (ILogger<Program> logger) =>
        {
            logger.LogInformation("Community policy/list");
            return Results.Ok(new { policies = Array.Empty<object>(), totalCount = 0, page = 1, pageSize = 20, mode = "community" });
        });

        app.MapGet("/agent/metrics/scorecard", (ILogger<Program> logger) =>
        {
            logger.LogInformation("Community metrics/scorecard");
            return Results.Ok(new { reliability = 0.0, efficiency = 0.0, safety = 0.0, antiLoop = 0.0, governance = 0.0, overall = 0.0, source = "community_fallback" });
        });

        app.MapGet("/memory/metrics", (ILogger<Program> logger) =>
        {
            logger.LogInformation("Community memory/metrics");
            return Results.Ok(new { totalChunks = 0, totalDocuments = 0, totalSizeBytes = 0, bySource = new Dictionary<string, int>(), oldestEntry = (DateTime?)null, newestEntry = (DateTime?)null });
        });

        app.MapGet("/memory/working", async (IEmbeddedKrnlAI kernel, CancellationToken ct) =>
        {
            var snapshot = await kernel.GetWorkingMemorySnapshotAsync(ct).ConfigureAwait(false);
            return Results.Ok(new
            {
                activeSlots = snapshot.ActiveSlots,
                maxSlots = snapshot.MaxSlots,
                cognitiveLoad = snapshot.CognitiveLoad,
                moment = snapshot.Moment,
                slots = snapshot.Slots.Select(slot => new
                {
                    slotId = slot.SlotId,
                    channelId = slot.ChannelId,
                    summary = slot.Summary,
                    relevance = slot.Relevance,
                    urgency = slot.Urgency
                })
            });
        });

        app.MapGet("/episodes/search", (ILogger<Program> logger) =>
        {
            logger.LogInformation("Community episodes/search");
            return Results.Ok(new { episodes = Array.Empty<object>(), totalCount = 0, page = 1, pageSize = 20 });
        });

        app.MapGet("/episodes/{id}", (string id, ILogger<Program> logger) =>
        {
            logger.LogInformation("Community episodes/{Id}", id);
            return Results.Ok(new { id, goalId = "community", status = "idle", createdAt = DateTime.UtcNow, finishedAt = (DateTime?)null, durationMs = (int?)null, outcome = (string?)null, successRate = (double?)null, summary = (string?)null, steps = Array.Empty<object>() });
        });

        app.MapGet("/observability/runtime/summary", async (IEmbeddedKrnlAI kernel, CancellationToken ct) =>
        {
            var goals = await kernel.GetKanbanGoalsAsync(ct).ConfigureAwait(false);
            return Results.Ok(new
            {
                gatewayHealthy = true,
                kernelHealthy = true,
                kernelVersion = "community",
                gatewayVersion = "sidecar-community",
                activeGoals = goals.Count,
                memoryUsageBytes = GC.GetTotalMemory(forceFullCollection: false),
                services = new Dictionary<string, string> { ["mode"] = "community", ["provider"] = kernel.Provider }
            });
        });

        app.MapGet("/goals/active", async (IEmbeddedKrnlAI kernel, CancellationToken ct) =>
        {
            var goals = await kernel.GetKanbanGoalsAsync(ct).ConfigureAwait(false);
            return Results.Ok(new
            {
                goals = goals.Select(goal => new
                {
                    id = goal.Id,
                    description = goal.Description,
                    status = goal.Status,
                    priority = goal.Priority,
                    createdAt = goal.CreatedAt,
                    deadline = goal.Deadline
                }),
                totalCount = goals.Count
            });
        });

        app.MapGet("/cognitive/dashboard", async (IEmbeddedKrnlAI kernel, CancellationToken ct) =>
        {
            var dashboard = await kernel.GetCognitiveDashboardAsync(ct).ConfigureAwait(false);
            return Results.Ok(new
            {
                overallHealth = dashboard.OverallHealth,
                activeModules = dashboard.ActiveModules.Select(module => new
                {
                    moduleName = module.ModuleName,
                    healthScore = module.HealthScore,
                    status = module.Status
                }),
                recentEvents = dashboard.RecentEvents.Select(cognitiveEvent => new
                {
                    eventType = cognitiveEvent.EventType,
                    description = cognitiveEvent.Description,
                    source = cognitiveEvent.Source,
                    timestamp = cognitiveEvent.Timestamp
                }),
                autonomyStatus = dashboard.Autonomy is null
                    ? null
                    : new
                    {
                        currentLevel = dashboard.Autonomy.CurrentLevel,
                        domains = dashboard.Autonomy.Domains.Select(domain => new
                        {
                            domain = domain.Domain,
                            level = domain.Level,
                            confidence = domain.Confidence
                        }),
                        recentDecisions = Array.Empty<object>(),
                        capturedAt = dashboard.Autonomy.CapturedAt
                    },
                agiAsiLevel = dashboard.AgiAsiLevel
            });
        });

        app.MapGet("/benchmark/summary", async (IEmbeddedKrnlAI kernel, CancellationToken ct) =>
        {
            var summary = await kernel.GetBenchmarkSummaryAsync(ct).ConfigureAwait(false);
            return Results.Ok(new
            {
                totalSuites = summary.TotalSuites,
                totalScenarios = summary.TotalScenarios,
                overallScore = summary.OverallScore,
                avgLatencyMs = summary.AvgLatencyMs,
                avgSuccessRate = summary.AvgSuccessRate,
                suites = summary.Suites.Select(suite => new
                {
                    name = suite.Name,
                    scenarios = suite.Scenarios,
                    score = suite.Score,
                    latencyMs = suite.LatencyMs,
                    successRate = suite.SuccessRate
                })
            });
        });

        app.MapGet("/causal/causes", () => Results.Ok(new
        {
            nodes = new[] { new { id = "cause-1", label = "Local Mode Active", type = "state", attributes = new Dictionary<string, double> { ["confidence"] = 0.95 } } },
            edges = new[] { new { sourceId = "cause-1", targetId = "effect-1", label = "enables", weight = 0.9 } }
        }));

        app.MapGet("/causal/predict", (string? action) => Results.Ok(new
        {
            action = action ?? "",
            outcome = "Local execution completed",
            probability = 0.85,
            contributingFactors = new[] { "local mode", "embedded kernel", "in-memory storage" }
        }));

        app.MapGet("/versions", () => Results.Ok(new { defaultVersion = "2.1.0", supportedVersions = new[] { "2.0.0", "2.1.0" }, legacyUnversionedDeprecated = false, legacySunsetDate = "" }));

        app.MapGet("/versions/contracts", () => Results.Ok(new
        {
            defaultApiVersion = "2.1",
            contracts = new[] { new { endpoint = "/agent/run", contractVersion = "2.1", supportedRange = "^2.0.0", deprecated = false, state = "active" } }
        }));

        app.MapGet("/archive/stats", () => Results.Ok(new { ok = true, totalArchived = 5, stores = new[] { "memory", "policies", "episodes" } }));

        app.MapGet("/snapshots", async (IEmbeddedKrnlAI kernel, CancellationToken ct) =>
        {
            var snapshots = await kernel.GetSnapshotsAsync(ct).ConfigureAwait(false);
            return Results.Ok(snapshots.Select(snapshot => new
            {
                snapshotId = snapshot.SnapshotId,
                label = snapshot.Label,
                scope = snapshot.Scope,
                reason = snapshot.Reason,
                componentCount = snapshot.ComponentCount,
                createdAt = snapshot.CreatedAt
            }));
        });
        app.MapGet("/objectives", () => Results.Ok(new[] { new { objectiveId = "obj-1", description = "Explore local mode", status = "active", progress = 0.6, priority = 3 } }));
        app.MapGet("/objectives/active", () => Results.Ok(new[] { new { objectiveId = "obj-1", description = "Explore local mode", status = "active", progress = 0.6, priority = 3 } }));
        app.MapGet("/investigations", () => Results.Ok(new[] { new { caseId = "inv-1", title = "System health check", status = "completed", evidenceCount = 8, createdAt = DateTime.UtcNow.AddHours(-2) } }));
        app.MapGet("/api/documents", async (int? limit, IEmbeddedKrnlAI kernel, CancellationToken ct) =>
        {
            var documents = await kernel.GetDocumentsAsync(limit ?? 50, ct).ConfigureAwait(false);
            return Results.Ok(documents.Select(document => new
            {
                documentId = document.DocumentId,
                fileName = document.FileName,
                fileSize = document.FileSize,
                format = document.Format,
                status = document.Status,
                errorMessage = document.ErrorMessage,
                chunkCount = document.ChunkCount,
                createdAt = document.CreatedAt,
                completedAt = document.CompletedAt
            }));
        });

        app.MapGet("/agent/status", () => Results.Ok(new { status = "idle", mode = "community" }));
        app.MapGet("/goals/list", () => Results.Ok(new { goals = Array.Empty<object>(), totalCount = 0 }));
        app.MapGet("/goals/{id}", (string id) => Results.Ok(new { id, status = "community", description = "" }));
        app.MapGet("/emotions/current", () => Results.Ok(new { valence = 0.0, arousal = 0.0, mode = "community" }));
        app.MapGet("/emotions/history", () => Results.Ok(new { entries = Array.Empty<object>(), mode = "community" }));
        app.MapGet("/metacognition/status", () => Results.Ok(new { overallHealth = 1.0, mode = "community" }));

        app.MapGet("/health", (IEmbeddedKrnlAI kernel, IOptions<SidecarOptions> opts) =>
        {
            var o = opts.Value;
            return Results.Ok(new
            {
                status = "healthy",
                mode = o.EffectiveMode,
                store = kernel.Options.StoreMode,
                vector = kernel.Options.VectorMode,
                skills = kernel.Options.SkillsStoreMode,
                llm = kernel.Provider,
                auth_configured = !string.IsNullOrWhiteSpace(o.Auth.Token) || !string.IsNullOrWhiteSpace(o.Enterprise.ApiKey),
                auth_endpoint = o.Auth.Endpoint ?? o.Enterprise.AuthEndpoint ?? "local",
                gateway_endpoint = o.Enterprise.GatewayEndpoint ?? "local",
                enterprise_tenant = o.Enterprise.Enabled ? o.Enterprise.TenantId ?? "default" : null,
            });
        });



        app.MapGet("/initiatives", (IEmbeddedKrnlAI kernel) =>
        {
            var pending = kernel.DrainInitiatives();
            return Results.Ok(new
            {
                ok = true,
                initiatives = pending.Select(message => new
                {
                    id = message.MessageId,
                    text = message.Text,
                    urgency = message.Urgency,
                    source = message.Source,
                    createdAt = message.CreatedAt
                })
            });
        });

        app.MapPost("/feedback", async (FeedbackRequest body, IEmbeddedKrnlAI kernel, CancellationToken ct) =>
        {
            await kernel.RecordFeedbackAsync(body.ResponseText ?? string.Empty, body.Positive, ct).ConfigureAwait(false);
            return Results.Ok(new { ok = true });
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = check => check.Tags.Contains("live") });

        return app;
    }
}

/// <summary>Feedback do usuário sobre uma resposta do kernel.</summary>
public sealed record FeedbackRequest(string? ResponseText, bool Positive);
