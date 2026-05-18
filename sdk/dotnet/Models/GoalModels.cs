using System.Text.Json.Serialization;

namespace KrnlAI.Sdk.Models;

public sealed record CreateGoalRequest(
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("priority")] double Priority,
    [property: JsonPropertyName("deadline")] DateTimeOffset? Deadline = null,
    [property: JsonPropertyName("parentGoalId")] string? ParentGoalId = null
);

public sealed record PersistentGoal(
    [property: JsonPropertyName("goalId")] string GoalId,
    [property: JsonPropertyName("parentGoalId")] string? ParentGoalId,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("progress")] double Progress,
    [property: JsonPropertyName("priority")] double Priority,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("deadline")] DateTimeOffset? Deadline,
    [property: JsonPropertyName("subGoalIds")] IReadOnlyList<string> SubGoalIds,
    [property: JsonPropertyName("dependsOnGoalIds")] IReadOnlyList<string> DependsOnGoalIds,
    [property: JsonPropertyName("successMetrics")] IReadOnlyDictionary<string, string> SuccessMetrics
);
