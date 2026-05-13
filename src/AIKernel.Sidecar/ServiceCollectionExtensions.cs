using System.Threading.RateLimiting;
using Kernel.Abstractions;
using Kernel.Core.Services.Safety;
using Kernel.Infrastructure.InMemory;
using Kernel.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AIKernel.Sidecar;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSidecarServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IAdversarialGuard, AdversarialGuard>();
        services.AddSingleton<IRiskScorer, SimpleRiskScorer>();
        services.AddSingleton<IPolicyStore, InMemoryPolicyStore>();
        services.AddSingleton<IStateStore, InMemoryStateStore>();
        services.Configure<RiskScorerOptions>(_ => { });

        // Options with validation
        services.AddOptions<SidecarOptions>()
            .Bind(configuration.GetSection(SidecarOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHealthChecks()
            .AddCheck<SidecarHealthCheck>("self", tags: ["live", "ready"]);

        // CORS
        services.AddCors(o =>
        {
            o.AddDefaultPolicy(p =>
            {
                var corsSection = configuration.GetSection("Sidecar:Cors:AllowedOrigins").Get<string[]>();
                if (corsSection is { Length: > 0 })
                    p.WithOrigins(corsSection).AllowAnyHeader().AllowAnyMethod();
                else if (environment.IsDevelopment())
                    p.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod();
            });
        });

        // Per-endpoint rate limiting
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;

            options.AddPolicy("health", _ =>
                RateLimitPartition.GetNoLimiter("health"));

            options.AddPolicy("agent-run", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = configuration.GetValue<int>("Sidecar:RateLimiting:AgentRunPermitLimit", 10),
                        Window = TimeSpan.FromSeconds(configuration.GetValue<int>("Sidecar:RateLimiting:WindowSeconds", 10)),
                        QueueLimit = 0
                    }));

            options.AddPolicy("memory-read", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = configuration.GetValue<int>("Sidecar:RateLimiting:MemoryReadPermitLimit", 30),
                        Window = TimeSpan.FromSeconds(configuration.GetValue<int>("Sidecar:RateLimiting:WindowSeconds", 10)),
                        QueueLimit = 0
                    }));

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = configuration.GetValue<int>("Sidecar:RateLimiting:GlobalPermitLimit", 60),
                        Window = TimeSpan.FromSeconds(configuration.GetValue<int>("Sidecar:RateLimiting:WindowSeconds", 10)),
                        QueueLimit = 0
                    }));
        });

        return services;
    }
}
