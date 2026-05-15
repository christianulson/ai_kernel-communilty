namespace Kernel.Contracts;

/// <summary>
/// Temporal query parameters for memory retrieval.
/// Extracted from MemoryQueryContract to reduce parameter count.
/// </summary>
public sealed record TemporalQueryHints(
    DateTimeOffset? AsOf = null,
    bool IncludeGraveyard = false,
    string? Intent = null,
    long? ReferenceSequence = null,
    string? ReferenceMomentId = null);
