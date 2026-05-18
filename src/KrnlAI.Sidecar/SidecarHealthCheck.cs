using KrnlAI.Core.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KrnlAI.Sidecar;

public sealed class SidecarHealthCheck(IAdversarialGuard guard) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        try
        {
            var result = await guard.ValidateAsync("ping", cancellationToken);
            return result.IsAllowed
                ? HealthCheckResult.Healthy("Safety guard operational")
                : HealthCheckResult.Degraded("Safety guard returned unexpected result");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Safety guard check failed", ex);
        }
    }
}
