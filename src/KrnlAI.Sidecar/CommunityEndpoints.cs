using KrnlAI.Embedded;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace KrnlAI.Sidecar;

public static class CommunityEndpoints
{
    public static WebApplication MapCommunityEndpoints(this WebApplication app)
    {
        app.UseRateLimiter();

        app.MapPost("/agent/run", async (AgentRunRequest request, EmbeddedKrnlAI kernel, CancellationToken ct) =>
        {
            var result = await kernel.RunAsync(request.Prompt ?? string.Empty, ct);
            return Results.Ok(new AgentRunResponse
            {
                Narration = result.Narration,
                Error = result.Error,
                TransportSteps = [new TransportStepDto { Label = "EmbeddedKrnlAI", Detail = result.Mode, Ok = result.Error is null }],
                ActiveStages = ["community"]
            });
        }).RequireRateLimiting("agent-run");

        app.MapPost("/memory/search", async (MemorySearchRequest request, EmbeddedKrnlAI kernel, CancellationToken ct) =>
        {
            var hits = await kernel.SearchMemoryAsync(request.Query, ct);
            return Results.Ok(new { hits, totalCount = hits.Count, queryTimeMs = 0.0, mode = "community" });
        });

        app.MapGet("/policy/list", () => Results.Ok(new
        {
            policies = Array.Empty<object>(),
            totalCount = 0,
            page = 1,
            pageSize = 20,
            mode = "community"
        }));

        app.MapGet("/agent/metrics/scorecard", () => Results.Ok(new
        {
            reliability = 0.0,
            efficiency = 0.0,
            safety = 0.0,
            antiLoop = 0.0,
            governance = 0.0,
            overall = 0.0,
            source = "community_fallback"
        }));

        app.MapGet("/memory/metrics", () => Results.Ok(new
        {
            totalChunks = 0,
            totalDocuments = 0,
            totalSizeBytes = 0,
            bySource = new Dictionary<string, int>(),
            oldestEntry = (DateTime?)null,
            newestEntry = (DateTime?)null
        }));

        app.MapGet("/memory/working", () => Results.Ok(new
        {
            activeSlots = 0,
            maxSlots = 0,
            slots = Array.Empty<object>()
        }));

        app.MapGet("/episodes/search", () => Results.Ok(new
        {
            episodes = Array.Empty<object>(),
            totalCount = 0,
            page = 1,
            pageSize = 20
        }));

        app.MapGet("/episodes/{id}", (string id) => Results.Ok(new
        {
            id,
            goalId = "community",
            status = "idle",
            createdAt = DateTime.UtcNow,
            finishedAt = (DateTime?)null,
            durationMs = (int?)null,
            outcome = (string?)null,
            successRate = (double?)null,
            summary = (string?)null,
            steps = Array.Empty<object>()
        }));

        app.MapGet("/observability/runtime/summary", () => Results.Ok(new
        {
            gatewayHealthy = true,
            kernelHealthy = true,
            kernelVersion = "community",
            gatewayVersion = "sidecar-community",
            activeGoals = 0,
            memoryUsageBytes = 0L,
            services = new Dictionary<string, string> { ["mode"] = "community" }
        }));

        app.MapGet("/goals/active", () => Results.Ok(new
        {
            goals = Array.Empty<object>(),
            totalCount = 0
        }));

        app.MapGet("/cognitive/dashboard", () => Results.Ok(new
        {
            overallHealth = 1.0,
            activeModules = Array.Empty<object>(),
            recentEvents = Array.Empty<object>(),
            autonomy = (object?)null
        }));

        app.MapGet("/benchmark/summary", () => Results.Ok(new
        {
            totalSuites = 0,
            totalScenarios = 0,
            overallScore = 0.0,
            avgLatencyMs = 0.0,
            avgSuccessRate = 0.0,
            suites = Array.Empty<object>()
        }));

        app.MapGet("/causal/causes", () => Results.Ok(new
        {
            nodes = Array.Empty<object>(),
            edges = Array.Empty<object>()
        }));

        app.MapGet("/causal/predict", (string? action) => Results.Ok(new
        {
            action = action ?? "",
            outcome = "unknown",
            probability = 0.0,
            contributingFactors = Array.Empty<string>()
        }));

        app.MapGet("/versions", () => Results.Ok(new
        {
            defaultVersion = "community",
            supportedVersions = new[] { "community" },
            legacyUnversionedDeprecated = false,
            legacySunsetDate = ""
        }));

        app.MapGet("/versions/contracts", () => Results.Ok(new
        {
            defaultApiVersion = "community",
            contracts = Array.Empty<object>()
        }));

        app.MapGet("/archive/stats", () => Results.Ok(new
        {
            ok = true,
            totalArchived = 0,
            stores = Array.Empty<string>()
        }));

        app.MapGet("/snapshots", () => Results.Ok(Array.Empty<object>()));

        app.MapGet("/objectives", () => Results.Ok(Array.Empty<object>()));
        app.MapGet("/objectives/active", () => Results.Ok(Array.Empty<object>()));

        app.MapGet("/investigations", () => Results.Ok(Array.Empty<object>()));

        app.MapGet("/api/documents", (int? limit) => Results.Ok(Array.Empty<object>()));

        app.MapGet("/health", (EmbeddedKrnlAI kernel) => Results.Ok(new
        {
            status = "healthy",
            mode = "community",
            store = kernel.Options.StoreMode,
            vector = kernel.Options.VectorMode,
            skills = kernel.Options.SkillsStoreMode,
            llm = kernel.Provider
        }));

        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = check => check.Tags.Contains("live") });

        return app;
    }
}

public sealed record MemorySearchRequest(string Query);
