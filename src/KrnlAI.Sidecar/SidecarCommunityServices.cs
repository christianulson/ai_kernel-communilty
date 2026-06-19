using System.Threading.RateLimiting;
using KrnlAI.Embedded;
using KrnlAI.Embedded.Abstractions;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace KrnlAI.Sidecar;

public static class SidecarCommunityServices
{
    public static IServiceCollection AddSidecarCommunityServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOptions<SidecarOptions>()
            .Bind(configuration.GetSection(SidecarOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IEmbeddedKrnlAI>(sp =>
        {
            var kernel = new EmbeddedKrnlAI(new EmbeddedKernelOptions
            {
                StoreMode = configuration["Store:Mode"] ?? "Sqlite",
                SqliteMode = configuration["Store:SqliteMode"] ?? "Hybrid",
                VectorMode = configuration["Vector:Mode"] ?? "Sqlite",
                CacheMode = configuration["Cache:Mode"] ?? "Memory",
                SkillsStoreMode = configuration["Skills:StoreMode"] ?? "Document",
                LLmProvider = configuration["LLM:Provider"] ?? "ollama"
            });
            var hostLifetime = sp.GetRequiredService<Microsoft.Extensions.Hosting.IHostApplicationLifetime>();
            hostLifetime.ApplicationStopping.Register(() => kernel.DisposeAsync().AsTask().GetAwaiter().GetResult());
            return kernel;
        });

        services.AddMemoryCache();

        // OpenTelemetry
        var otlpEndpoint = configuration.GetValue<string>("Sidecar:Otlp:Endpoint");
        var hasOtlp = !string.IsNullOrWhiteSpace(otlpEndpoint);

        services.AddOpenTelemetry()
            .ConfigureResource(r =>
            {
                r.AddService("KrnlAI.Sidecar", "1.0.0",
                    autoGenerateServiceInstanceId: false);
                r.AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment.EnvironmentName,
                    ["host.name"] = Environment.MachineName,
                    ["sidecar.mode"] = "Community",
                });
            })
            .WithTracing(t =>
            {
                t.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .SetSampler(new AlwaysOnSampler());
                if (hasOtlp)
                    t.AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri(otlpEndpoint!);
                        var headers = configuration.GetValue<string>("Sidecar:Otlp:Headers");
                        if (!string.IsNullOrWhiteSpace(headers))
                            o.Headers = headers;
                    });
            })
            .WithMetrics(m =>
            {
                m.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddPrometheusExporter();
                if (hasOtlp)
                    m.AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri(otlpEndpoint!);
                        var headers = configuration.GetValue<string>("Sidecar:Otlp:Headers");
                        if (!string.IsNullOrWhiteSpace(headers))
                            o.Headers = headers;
                    });
            });

        services.AddHealthChecks().AddCheck("community", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("community"), ["live", "ready"]);

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

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;

            options.AddPolicy("health", _ =>
                RateLimitPartition.GetNoLimiter("health"));

            options.AddFixedWindowLimiter("agent-run", limiter =>
            {
                limiter.PermitLimit = configuration.GetValue<int>("Sidecar:RateLimiting:AgentRunPermitLimit", 10);
                limiter.Window = TimeSpan.FromSeconds(configuration.GetValue<int>("Sidecar:RateLimiting:WindowSeconds", 10));
            });

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
