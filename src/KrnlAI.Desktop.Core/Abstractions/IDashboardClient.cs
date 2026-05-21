namespace KrnlAI.Desktop.Core.Abstractions;

public interface IDashboardClient
{
    Task<Models.AgentMetricsSummary?> GetMetricsSummaryAsync(CancellationToken cancellationToken = default);
    Task<Models.AgentScorecard?> GetScorecardAsync(CancellationToken cancellationToken = default);
    Task<Models.RuntimeSummary?> GetRuntimeSummaryAsync(CancellationToken cancellationToken = default);
}
