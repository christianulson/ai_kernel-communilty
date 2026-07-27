using System.Threading.RateLimiting;
using KrnlAI.Core.Abstractions.Policy;
using KrnlAI.Core.Abstractions.Risk;
using KrnlAI.Core.Abstractions.State;
using KrnlAI.Infrastructure.InMemory;
using KrnlAI.Core.Services;
using KrnlAI.Embedded.Abstractions;
using KrnlAI.Embedded.Services;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using KrnlAI.Core.Abstractions.Safety;
using KrnlAI.Safety.Services;
using KrnlAI.Sidecar.Services;

namespace KrnlAI.Sidecar;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSidecarServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IAdversarialGuard, AdversarialGuard>();
        services.AddSingleton<IRiskScorer, SimpleRiskScorer>();
        services.AddSingleton<IPolicyStore, InMemoryPolicyStore>();
        services.AddSingleton<IStateStore, InMemoryStateStore>();
        // Options with validation
        services.AddOptions<SidecarOptions>()
            .Bind(configuration.GetSection(SidecarOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Safety pipeline
        services.AddSingleton<IFundamentalRulesEngine, FundamentalRulesEngine>();
        services.AddSingleton<IEthicalEnforcer, EthicalEnforcer>();
        services.AddSingleton<ISemanticSimilarityScorer>(sp => new SemanticSimilarityScorer(FundamentalRulesEngine.GetAllKeywords()));
        services.AddSingleton<IHybridSafetyEngine, HybridSafetyEngine>();
        services.AddSingleton<ILawEnforcer, LawEnforcer>();

        // OpenTelemetry
        var otlpEndpoint = configuration.GetValue<string>("Sidecar:Otlp:Endpoint");
        var hasOtlp = !string.IsNullOrWhiteSpace(otlpEndpoint);

        services.AddOpenTelemetry()
            .ConfigureResource(r =>
            {
                r.AddService("KrnlAI.Sidecar", "1.0.0",
                    autoGenerateServiceInstanceId: false);
                var mode = configuration.GetValue<string>("Sidecar:Enterprise:Enabled") == "true" ? "enterprise" : "community";
                r.AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment.EnvironmentName,
                    ["host.name"] = Environment.MachineName,
                    ["sidecar.mode"] = mode,
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

        // Memory cache for proxy fallback
        services.AddMemoryCache();

        // ── Tool Services ──
        services.AddSingleton<IConversationStore, InMemoryConversationStore>();
        services.AddHttpClient("search")
            .ConfigureHttpClient(c =>
            {
                c.DefaultRequestHeaders.UserAgent.ParseAdd("KrnlAI-Sidecar/1.0");
                c.Timeout = TimeSpan.FromSeconds(15);
            });
        services.AddSingleton<ISearchService, SearchService>();

        // EmbeddedKrnlAI for local fallback when proxy is unavailable
        services.AddSingleton<IEmbeddedKrnlAI, EmbeddedKrnlAI>();

        // Named HTTP client for LLM calls (chat bridge)
        services.AddHttpClient("deepseek-chat")
            .ConfigureHttpClient(c =>
            {
                c.Timeout = TimeSpan.FromSeconds(180);
            });

        // KrnlAI API proxy (optional — for proxying to remote KrnlAI API)
        services.AddHttpClient("kernel")
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<SidecarOptions>>();
                if (!string.IsNullOrWhiteSpace(options.Value.KernelApi.BaseUrl))
                {
                    client.BaseAddress = new Uri(options.Value.KernelApi.BaseUrl.TrimEnd('/'));
                    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.Value.KernelApi.TimeoutSeconds));
                }
            });
        services.AddSingleton<KernelApiProxy>();

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
