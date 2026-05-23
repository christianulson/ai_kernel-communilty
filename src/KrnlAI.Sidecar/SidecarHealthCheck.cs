using KrnlAI.Core.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace KrnlAI.Sidecar;

public sealed class SidecarHealthCheck : IHealthCheck
{
    private readonly IAdversarialGuard _guard;
    private readonly KernelApiProxy _proxy;
    private readonly IOptions<SidecarOptions> _options;

    public SidecarHealthCheck(IAdversarialGuard guard, KernelApiProxy proxy, IOptions<SidecarOptions> options)
    {
        _guard = guard;
        _proxy = proxy;
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, object>();

        // Self-check: safety guard
        try
        {
            var result = await _guard.ValidateAsync("ping", cancellationToken);
            data["safety_guard"] = result.IsAllowed ? "healthy" : "degraded";
        }
        catch (Exception ex)
        {
            data["safety_guard"] = $"unhealthy: {ex.Message}";
        }

        // Proxy target health check
        if (_proxy.IsConfigured)
        {
            if (context.Registration.Tags.Contains("ready"))
            {
                var proxyHealthy = await _proxy.PingAsync(cancellationToken);
                data["proxy_target"] = proxyHealthy ? "healthy" : "unhealthy";
                if (!proxyHealthy)
                {
                    return HealthCheckResult.Unhealthy("Kernel API target unreachable", data: data);
                }
            }
            else
            {
                data["proxy_target"] = "configured";
            }
        }

        return HealthCheckResult.Healthy("Sidecar operational", data: data);
    }
}
