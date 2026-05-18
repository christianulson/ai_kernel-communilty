using KrnlAI.Embedded;
using Microsoft.AspNetCore.RateLimiting;

namespace KrnlAI.Sidecar;

public static class SidecarCommunityServices
{
    public static IServiceCollection AddSidecarCommunityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(new EmbeddedKernel(new EmbeddedKernelOptions
        {
            StoreMode = configuration["Store:Mode"] ?? "SQLite",
            SqliteMode = configuration["Store:SqliteMode"] ?? "Hybrid",
            VectorMode = configuration["Vector:Mode"] ?? "Sqlite",
            CacheMode = configuration["Cache:Mode"] ?? "Memory",
            SkillsStoreMode = configuration["Skills:StoreMode"] ?? "Document",
            LLmProvider = configuration["LLM:Provider"] ?? "ollama"
        }));

        services.AddHealthChecks().AddCheck("community", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("community"), ["live", "ready"]);
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("agent-run", limiter =>
            {
                limiter.PermitLimit = 10;
                limiter.Window = TimeSpan.FromSeconds(10);
            });
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });
        return services;
    }
}
