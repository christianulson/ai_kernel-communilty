using AIKernel.Embedded;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace AIKernel.Sidecar;

public static class CommunityEndpoints
{
    public static WebApplication MapCommunityEndpoints(this WebApplication app)
    {
        app.UseRateLimiter();

        app.MapPost("/agent/run", async (AgentRunRequest request, EmbeddedKernel kernel, CancellationToken ct) =>
        {
            var result = await kernel.RunAsync(request.Prompt ?? string.Empty, ct);
            return Results.Ok(new AgentRunResponse
            {
                Narration = result.Narration,
                Error = result.Error,
                TransportSteps = [new TransportStepDto { Label = "EmbeddedKernel", Detail = result.Mode, Ok = result.Error is null }],
                ActiveStages = ["community"]
            });
        }).RequireRateLimiting("agent-run");

        app.MapPost("/memory/search", async (MemorySearchRequest request, EmbeddedKernel kernel, CancellationToken ct) =>
        {
            var hits = await kernel.SearchMemoryAsync(request.Query, ct);
            return Results.Ok(new { hits, totalCount = hits.Count, mode = "community" });
        });

        app.MapGet("/health", (EmbeddedKernel kernel) => Results.Ok(new
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
