using Refit;

namespace Kernel.Contracts.Abstractions;

public interface IKernelEpisodesApi
{
    [Post("/episodes")]
    Task<SaveEpisodeResponse> SaveAsync([Body] SaveEpisodeRequest request, CancellationToken ct);

    [Get("/episodes")]
    Task<EpisodeListResponse> ListRecentAsync(int take, CancellationToken ct);

    [Get("/episodes/{episodeId}")]
    Task<EpisodeDetailResponse> GetAsync(string episodeId, CancellationToken ct);
}

public sealed record SaveEpisodeRequest(
    string? EpisodeId,
    string UserId,
    string? TenantPlan,
    string Goal,
    string? GoalId,
    string PlanJson,
    string StepsJson,
    string Status,
    string Summary,
    string? Domain,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? FinishedAt,
    string? SelfCheckJson,
    string? PostMortemJson,
    string? ProfileJson,
    string? BudgetJson,
    string? MetricsJson,
    string? PromptCapability,
    string? PromptVersion,
    string? Provider,
    string? Model,
    string? PromptMetadataJson,
    string? MetacognitiveReviewJson);

public sealed record SaveEpisodeResponse(bool Ok, string EpisodeId);
public sealed record EpisodeListResponse(bool Ok, List<EpisodeItem> Items);
public sealed record EpisodeItem(string EpisodeId, string Goal, string Status, DateTimeOffset CreatedAt);
public sealed record EpisodeDetailResponse(bool Ok, EpisodeFull Item);
public sealed record EpisodeFull(string EpisodeId, string Goal, string Status, string Summary, DateTimeOffset CreatedAt, DateTimeOffset? FinishedAt, List<EpisodeStepItem> Steps);
public sealed record EpisodeStepItem(int StepIndex, string Tool, string Status, string? Error);
