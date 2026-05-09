using Refit;

namespace Kernel.Contracts.Abstractions;

public interface IKernelObservabilityApi
{
    [Get("/observability/governance/summary")]
    Task<GovernanceSummaryDto> GetGovernanceSummaryAsync(CancellationToken ct);

    [Get("/observability/runtime/summary")]
    Task<RuntimeSummaryDto> GetRuntimeSummaryAsync(CancellationToken ct);

    [Get("/cognitive/dashboard")]
    Task<CognitiveDashboardDto> GetCognitiveDashboardAsync(CancellationToken ct);
}

public sealed record GovernanceSummaryDto(string Status, int ActivePolicies, DateTimeOffset LastUpdated);
public sealed record RuntimeSummaryDto(double OverallHealth, int ActiveGoals, int TotalCyclesRun);
public sealed record CognitiveDashboardDto(double OverallHealth, List<ModuleHealthDto> ActiveModules, List<CognitiveEventDto> RecentEvents, AutonomyStatusDto? Autonomy, DateTimeOffset LastUpdated);
public sealed record ModuleHealthDto(string ModuleName, double HealthScore, string Status);
public sealed record CognitiveEventDto(string EventType, string Source, string Description, DateTimeOffset Timestamp);
public sealed record AutonomyStatusDto(string CurrentLevel, DateTimeOffset LastUpdated);
