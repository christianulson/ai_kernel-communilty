namespace Kernel.Contracts;

/// <summary>
/// Memory scopes that can participate in a unified retrieval request.
/// </summary>
[Flags]
public enum MemoryScope
{
    /// <summary>
    /// No memory scope selected.
    /// </summary>
    None = 0,

    /// <summary>
    /// Semantic or document memory.
    /// </summary>
    Semantic = 1,

    /// <summary>
    /// Episodic execution memory.
    /// </summary>
    Episodic = 2,

    /// <summary>
    /// Procedural memory and learned procedures.
    /// </summary>
    Procedural = 4,

    /// <summary>
    /// Current working memory.
    /// </summary>
    Working = 8,

    /// <summary>
    /// All supported memory scopes.
    /// </summary>
    All = Semantic | Episodic | Procedural | Working
}

/// <summary>
/// Canonical query for unified cognitive memory retrieval.
/// </summary>
/// <param name="Query">Natural language or structured query text.</param>
/// <param name="UserId">Optional user identifier.</param>
/// <param name="TenantId">Optional tenant identifier.</param>
/// <param name="Scopes">Memory scopes that should be searched.</param>
/// <param name="TopK">Maximum number of results.</param>
/// <param name="MinScore">Minimum normalized score.</param>
/// <param name="AsOf">Optional temporal query timestamp.</param>
/// <param name="RequiredEvidenceIds">Evidence identifiers that must be considered or linked.</param>
/// <param name="Metadata">Small key/value metadata for routing and audit.</param>
public sealed record MemoryQueryContract(
    string Query,
    string? UserId,
    string? TenantId,
    MemoryScope Scopes,
    int TopK,
    double MinScore,
    DateTimeOffset? AsOf,
    IReadOnlyList<string> RequiredEvidenceIds,
    IReadOnlyDictionary<string, string> Metadata,
    bool IncludeGraveyard = false);

/// <summary>
/// Canonical memory retrieval hit with provenance.
/// </summary>
/// <param name="MemoryId">Stable memory item identifier.</param>
/// <param name="Scope">Memory scope that produced the hit.</param>
/// <param name="Text">Text or summary returned by retrieval.</param>
/// <param name="Score">Normalized retrieval score.</param>
/// <param name="Source">Source store or subsystem.</param>
/// <param name="EvidenceIds">Evidence identifiers supporting the hit.</param>
/// <param name="Metadata">Small key/value metadata for routing and audit.</param>
/// <param name="ObservedAt">When the memory was observed (null if unknown).</param>
/// <param name="CreatedAt">When the memory was created (null if unknown).</param>
/// <param name="LastAccessedAt">When the memory was last accessed (null if unknown).</param>
/// <param name="MomentId">Cognitive moment id when observed (null if unbound).</param>
/// <param name="MomentSequence">Monotonic moment sequence when observed (null if unbound).</param>
public sealed record MemoryHitContract(
    string MemoryId,
    MemoryScope Scope,
    string Text,
    double Score,
    string Source,
    IReadOnlyList<string> EvidenceIds,
    IReadOnlyDictionary<string, string> Metadata,
    DateTimeOffset? ObservedAt = null,
    DateTimeOffset? CreatedAt = null,
    DateTimeOffset? LastAccessedAt = null,
    string? MomentId = null,
    long? MomentSequence = null);
