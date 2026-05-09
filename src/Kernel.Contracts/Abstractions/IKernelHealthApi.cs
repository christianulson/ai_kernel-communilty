using Refit;

namespace Kernel.Contracts.Abstractions;

public interface IKernelHealthApi
{
    [Get("/health")]
    Task<HealthResponse> GetHealthAsync(CancellationToken ct);

    [Get("/health/ready")]
    Task<HealthResponse> GetReadyAsync(CancellationToken ct);

    [Get("/health/live")]
    Task<HealthResponse> GetLiveAsync(CancellationToken ct);
}

public sealed record HealthResponse(bool Ok, DateTimeOffset Ts);
