namespace LLMGateway.Api.Contracts.Learning;

/// <summary>
/// Canonical journal entry used to persist the learning trail.
/// </summary>
/// <param name="JournalId">Deterministic idempotency key for the entry.</param>
/// <param name="EntryType">Logical type of learning event.</param>
/// <param name="EpisodeId">Optional episode or scenario identifier.</param>
/// <param name="PolicyDomain">Optional policy domain associated with the event.</param>
/// <param name="ActionType">Optional action type associated with the event.</param>
/// <param name="ScenarioCode">Optional scenario code associated with the event.</param>
/// <param name="EvidenceIds">Evidence identifiers linked to the event.</param>
/// <param name="PayloadJson">Serialized payload for audit and replay.</param>
/// <param name="RecordedAt">Timestamp when the event was recorded.</param>
public sealed record LearningJournalEntry(
    string JournalId,
    string EntryType,
    string? EpisodeId,
    string? PolicyDomain,
    string? ActionType,
    string? ScenarioCode,
    IReadOnlyList<string> EvidenceIds,
    string PayloadJson,
    DateTimeOffset RecordedAt);
