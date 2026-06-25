using KrnlAI.Core.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace KrnlAI.Sidecar;

public sealed class SidecarHealthCheck(IAdversarialGuard guard, KernelApiProxy proxy, IOptions<SidecarOptions> options) : IHealthCheck
{
    private readonly IOptions<SidecarOptions> _options = options;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var mode = options.EffectiveMode;
        var data = new Dictionary<string, object>
        {
            ["mode"] = mode,
            ["auth_configured"] = !string.IsNullOrWhiteSpace(options.Auth.Token) || !string.IsNullOrWhiteSpace(options.Enterprise.ApiKey),
            ["auth_endpoint"] = options.Auth.Endpoint ?? options.Enterprise.AuthEndpoint ?? "local",
            ["gateway_endpoint"] = options.Enterprise.GatewayEndpoint ?? "local",
        };

        if (mode == "enterprise")
        {
            data["enterprise_tenant"] = options.Enterprise.TenantId ?? "default";
            data["enterprise_api_key_configured"] = !string.IsNullOrWhiteSpace(options.Enterprise.ApiKey);
        }

        // Self-check: safety guard
        try
        {
            var result = await guard.ValidateAsync("ping", cancellationToken).ConfigureAwait(false);
            data["safety_guard"] = result.IsAllowed ? "healthy" : "degraded";
        }
        catch (Exception ex)
        {
            data["safety_guard"] = $"unhealthy: {ex.Message}";
        }

        // Proxy target health check
        if (proxy.IsConfigured)
        {
            if (context.Registration.Tags.Contains("ready"))
            {
                var proxyHealthy = await proxy.PingAsync(cancellationToken).ConfigureAwait(false);
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

        return HealthCheckResult.Healthy($"Sidecar operational ({mode} mode)", data: data);
    }
}
