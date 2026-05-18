using System.Text.Json.Serialization;

namespace KrnlAi.Sdk.Models;

public sealed record EpisodeListItem(
    [property: JsonPropertyName("episodeId")] string EpisodeId,
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("goal")] string Goal,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("domain")] string Domain,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("finishedAt")] DateTimeOffset? FinishedAt = null,
    [property: JsonPropertyName("goalId")] string? GoalId = null
);

public sealed record EpisodeDetail(
    [property: JsonPropertyName("episodeId")] string EpisodeId,
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("goal")] string Goal,
    [property: JsonPropertyName("planJson")] string PlanJson,
    [property: JsonPropertyName("stepsJson")] string StepsJson,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("domain")] string Domain,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("finishedAt")] DateTimeOffset? FinishedAt = null,
    [property: JsonPropertyName("selfCheckJson")] string? SelfCheckJson = null,
    [property: JsonPropertyName("postMortemJson")] string? PostMortemJson = null,
    [property: JsonPropertyName("metricsJson")] string? MetricsJson = null,
    [property: JsonPropertyName("goalId")] string? GoalId = null
);
