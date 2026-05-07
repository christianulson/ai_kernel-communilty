namespace Kernel.Contracts;

/// <summary>
/// Canonical learning episode used to preserve goal, plan, outcome and evidence together.
/// </summary>
/// <param name="EpisodeId">Stable learning episode identifier.</param>
/// <param name="UserId">Identifier of the user or tenant owner.</param>
/// <param name="GoalId">Optional linked goal identifier.</param>
/// <param name="Goal">Goal description that motivated the episode.</param>
/// <param name="Plan">Structured plan executed during the episode.</param>
/// <param name="Outcome">Structured outcome captured after execution.</param>
/// <param name="Evidence">Evidence items attached to the episode.</param>
/// <param name="CreatedAt">Timestamp when the episode was created.</param>
/// <param name="CompletedAt">Optional timestamp when the episode completed.</param>
/// <param name="Metadata">Small key/value metadata for audit and routing.</param>
public sealed record LearningEpisodeContract(
    string EpisodeId,
    string UserId,
    string? GoalId,
    string Goal,
    LearningPlanContract Plan,
    LearningOutcomeContract Outcome,
    IReadOnlyList<LearningEvidenceContract> Evidence,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyDictionary<string, string> Metadata);

/// <summary>
/// Structured plan snapshot stored with a learning episode.
/// </summary>
/// <param name="Goal">Plan goal.</param>
/// <param name="Steps">Structured plan steps.</param>
public sealed record LearningPlanContract(
    string Goal,
    IReadOnlyList<LearningPlanStepContract> Steps);

/// <summary>
/// Structured step inside a learning episode plan.
/// </summary>
/// <param name="StepIndex">Zero-based step index.</param>
/// <param name="Tool">Tool or action name.</param>
/// <param name="InputJson">Serialized step input payload.</param>
/// <param name="ExpectedOutcome">Expected result for the step.</param>
/// <param name="Risk">Risk level attached to the step.</param>
/// <param name="RollbackHint">Optional rollback hint.</param>
/// <param name="IdempotencyKey">Optional idempotency key.</param>
public sealed record LearningPlanStepContract(
    int StepIndex,
    string Tool,
    string InputJson,
    string ExpectedOutcome,
    string Risk,
    string? RollbackHint,
    string? IdempotencyKey);

/// <summary>
/// Structured outcome stored with a learning episode.
/// </summary>
/// <param name="Status">Final episode status.</param>
/// <param name="Summary">Human-readable summary.</param>
/// <param name="Success">Whether the episode succeeded.</param>
/// <param name="KeyFindings">Main findings extracted from the run.</param>
/// <param name="Errors">Observed errors or failure markers.</param>
/// <param name="LessonsLearned">Reusable lessons extracted from the episode.</param>
/// <param name="EvidenceIds">Evidence identifiers supporting the outcome.</param>
/// <param name="Metadata">Small key/value metadata for audit and routing.</param>
public sealed record LearningOutcomeContract(
    string Status,
    string Summary,
    bool Success,
    IReadOnlyList<string> KeyFindings,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> LessonsLearned,
    IReadOnlyList<string> EvidenceIds,
    IReadOnlyDictionary<string, string> Metadata);

/// <summary>
/// Canonical evidence item attached to a learning episode.
/// </summary>
/// <param name="EvidenceId">Stable evidence identifier.</param>
/// <param name="SourceType">Evidence source type, for example episode or tool.</param>
/// <param name="SourceId">Stable source identifier.</param>
/// <param name="EpisodeId">Optional episode identifier linked to the evidence.</param>
/// <param name="Confidence">Normalized confidence from 0.0 to 1.0.</param>
/// <param name="ObservedAt">Timestamp when the evidence was observed.</param>
/// <param name="Metadata">Small key/value metadata for audit and routing.</param>
public sealed record LearningEvidenceContract(
    string EvidenceId,
    string SourceType,
    string SourceId,
    string? EpisodeId,
    double Confidence,
    DateTimeOffset ObservedAt,
    IReadOnlyDictionary<string, string> Metadata);
