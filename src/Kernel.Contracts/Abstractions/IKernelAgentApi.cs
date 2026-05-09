using Refit;

namespace Kernel.Contracts.Abstractions;

public interface IKernelAgentApi
{
    [Post("/agent/run")]
    Task<AgentRunResponse> RunAgentAsync([Body] AgentRunRequest request, CancellationToken ct);

    [Post("/agent/feedback")]
    Task<FeedbackResponse> SubmitFeedbackAsync([Body] FeedbackRequest request, CancellationToken ct);

    [Get("/agent/metrics/summary")]
    Task<AgentMetricsSummary> GetMetricsSummaryAsync(CancellationToken ct);

    [Get("/agent/metrics/scorecard")]
    Task<AgentScorecard> GetScorecardAsync(CancellationToken ct);

    [Get("/agent/metrics/summary/by-goal")]
    Task<MetricsByGoalResponse> GetMetricsByGoalAsync(CancellationToken ct);

    [Get("/agent/metrics/cycles")]
    Task<GoalCycleList> GetGoalCyclesAsync(string goalId, CancellationToken ct);
}

public sealed record AgentRunRequest(string Prompt, string Mode = "gateway", string? AgentId = null, Dictionary<string, string>? Metadata = null);
public sealed record AgentRunResponse(string? Narration, Dictionary<string, object>? Command, List<TransportStep>? TransportSteps, List<string>? ActiveStages, string? Error);
public sealed record TransportStep(string Label, string Detail, bool Ok, int? Status);
public sealed record FeedbackRequest(string EpisodeId, int Rating, string? Comment, string? Category);
public sealed record FeedbackResponse(bool Success, string? FeedbackId, string? Message);
public sealed record AgentMetricsSummary(int TotalRuns, int CompletedRuns, int FailedRuns, int AbortedRuns, double SuccessRate, double AvgLatencyMs, double AvgCost);
public sealed record AgentScorecard(double Reliability, double Efficiency, double Safety, double AntiLoop, double Governance, double Overall);
public sealed record MetricsByGoalResponse(List<GoalMetrics> Goals, int TotalCount);
public sealed record GoalMetrics(string GoalId, int TotalRuns, int CompletedRuns, int FailedRuns, double SuccessRate, double AvgLatencyMs, double AvgEstimatedCost);
public sealed record GoalCycleList(List<GoalCycleSummary> Cycles);
public sealed record GoalCycleSummary(string GoalId, string Action, string Status, DateTime Timestamp, int DurationMs);
