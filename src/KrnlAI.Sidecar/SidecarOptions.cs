using System.ComponentModel.DataAnnotations;

namespace KrnlAI.Sidecar;

public sealed class SidecarOptions
{
    public const string SectionName = "Sidecar";

    public AuthOptions Auth { get; init; } = new();
    public RateLimitingOptions RateLimiting { get; init; } = new();
    public CorsOptions Cors { get; init; } = new();
    public KernelApiOptions KernelApi { get; init; } = new();
    public AgentRunOptions AgentRun { get; init; } = new();
    public OtlpOptions Otlp { get; init; } = new();
}

public sealed class AuthOptions
{
    public string? Token { get; init; }
}

public sealed class RateLimitingOptions
{
    [Range(1, 1000)]
    public int GlobalPermitLimit { get; init; } = 60;
    [Range(1, 3600)]
    public int WindowSeconds { get; init; } = 10;
    [Range(1, 100)]
    public int AgentRunPermitLimit { get; init; } = 10;
    [Range(1, 200)]
    public int MemoryReadPermitLimit { get; init; } = 30;
}

public sealed class CorsOptions
{
    public string[]? AllowedOrigins { get; init; }
}

public sealed class KernelApiOptions
{
    public string BaseUrl { get; init; } = "";
    public int TimeoutSeconds { get; init; } = 10;
    public int CacheTtlSeconds { get; init; } = 60;
    public int RetryCount { get; init; } = 3;
    public int CircuitBreakDurationSeconds { get; init; } = 15;
    public int CircuitMinThroughput { get; init; } = 5;
    public double CircuitFailureRatio { get; init; } = 0.5;
}

public sealed class AgentRunOptions
{
    public int TimeoutSeconds { get; init; } = 120;
}

public sealed class OtlpOptions
{
    public string Endpoint { get; init; } = "";
    public string Headers { get; init; } = "";
    public string Protocol { get; init; } = "Grpc";
}
