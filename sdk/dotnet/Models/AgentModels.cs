using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiKernel.Sdk.Models;

public sealed record BudgetPolicy(
    [property: JsonPropertyName("maxToolCalls")] int MaxToolCalls = 12,
    [property: JsonPropertyName("maxTokens")] int MaxTokens = 4000,
    [property: JsonPropertyName("maxLatencyMs")] int MaxLatencyMs = 30000,
    [property: JsonPropertyName("maxEstimatedCost")] decimal MaxEstimatedCost = 0m
);

public sealed record AgentRunRequest(
    [property: JsonPropertyName("goal")] string Goal,
    [property: JsonPropertyName("userId")] string? UserId = null,
    [property: JsonPropertyName("goalId")] string? GoalId = null,
    [property: JsonPropertyName("maxSteps")] int? MaxSteps = null,
    [property: JsonPropertyName("provider")] string? Provider = null,
    [property: JsonPropertyName("model")] string? Model = null,
    [property: JsonPropertyName("approveHighRisk")] bool ApproveHighRisk = false,
    [property: JsonPropertyName("approveMetaCriticStops")] bool ApproveMetaCriticStops = false,
    [property: JsonPropertyName("metaCriticOverrideReason")] string? MetaCriticOverrideReason = null,
    [property: JsonPropertyName("budget")] BudgetPolicy? Budget = null
);

public sealed record PlanStepResult(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("tool")] string Tool,
    [property: JsonPropertyName("risk")] string Risk,
    [property: JsonPropertyName("rollbackHint")] string? RollbackHint,
    [property: JsonPropertyName("rawResult")] System.Text.Json.JsonElement RawResult,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("error")] string? Error = null,
    [property: JsonPropertyName("idempotencyKey")] string? IdempotencyKey = null
);

public sealed record AgentRunResponse(
    [property: JsonPropertyName("goal")] string Goal,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("steps")] IReadOnlyList<PlanStepResult> Steps,
    [property: JsonPropertyName("episodeId")] string? EpisodeId = null,
    [property: JsonPropertyName("provider")] string? Provider = null,
    [property: JsonPropertyName("model")] string? Model = null,
    [property: JsonPropertyName("goalId")] string? GoalId = null
);

public sealed record AgentRunStatus(
    [property: JsonPropertyName("runId")] string RunId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("goal")] string Goal,
    [property: JsonPropertyName("startedAt")] string StartedAt,
    [property: JsonPropertyName("summary")] string? Summary = null,
    [property: JsonPropertyName("progress")] double? Progress = null,
    [property: JsonPropertyName("finishedAt")] string? FinishedAt = null,
    [property: JsonPropertyName("error")] string? Error = null
);
