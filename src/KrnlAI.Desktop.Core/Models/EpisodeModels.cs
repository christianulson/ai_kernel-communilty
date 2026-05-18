namespace KrnlAI.Desktop.Core.Models;

public record EpisodeInfo(
    string Id,
    string GoalId,
    string Status,
    DateTime CreatedAt,
    DateTime? FinishedAt,
    int? DurationMs,
    string? Outcome,
    double? SuccessRate
);

public record EpisodeDetails(
    string Id,
    string GoalId,
    string Status,
    DateTime CreatedAt,
    DateTime? FinishedAt,
    int? DurationMs,
    string? Outcome,
    double? SuccessRate,
    string? Summary,
    List<EpisodeStep>? Steps
);

public record EpisodeStep(
    int StepIndex,
    string Label,
    string Detail,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    int? DurationMs,
    bool Ok,
    string? Error
);

public record EpisodeSearchResult(
    List<EpisodeInfo> Episodes,
    int TotalCount,
    int Page,
    int PageSize
);

public record EpisodeSearchRequest(
    string? Query = null,
    string? GoalId = null,
    string? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 20
);