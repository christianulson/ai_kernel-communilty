namespace Kernel.Contracts;

/// <summary>
/// Canonical named entity in the world model.
/// </summary>
/// <param name="EntityId">Stable entity identifier.</param>
/// <param name="EntityType">Entity type, for example service, metric, user, concept, or domain.</param>
/// <param name="Label">Human-readable entity label.</param>
/// <param name="Aliases">Known aliases for matching and retrieval.</param>
/// <param name="Salience">Normalized current importance from 0.0 to 1.0.</param>
/// <param name="ObservedAt">Timestamp when the entity was last observed.</param>
/// <param name="Metadata">Small key/value metadata for audit and routing.</param>
public sealed record WorldEntityContract(
    string EntityId,
    string EntityType,
    string Label,
    IReadOnlyList<string> Aliases,
    double Salience,
    DateTimeOffset ObservedAt,
    IReadOnlyDictionary<string, string> Metadata);

/// <summary>
/// Canonical relation between two world model entities.
/// </summary>
/// <param name="RelationId">Stable relation identifier.</param>
/// <param name="FromEntityId">Source entity identifier.</param>
/// <param name="ToEntityId">Target entity identifier.</param>
/// <param name="RelationType">Relation type, for example causes, contains, depends_on, or influences.</param>
/// <param name="Confidence">Normalized confidence from 0.0 to 1.0.</param>
/// <param name="ValidFrom">Timestamp from which the relation is valid.</param>
/// <param name="ValidUntil">Optional timestamp when the relation stops being valid.</param>
/// <param name="EvidenceIds">Evidence identifiers supporting the relation.</param>
/// <param name="Metadata">Small key/value metadata for audit and routing.</param>
public sealed record WorldRelationContract(
    string RelationId,
    string FromEntityId,
    string ToEntityId,
    string RelationType,
    double Confidence,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    IReadOnlyList<string> EvidenceIds,
    IReadOnlyDictionary<string, string> Metadata);

/// <summary>
/// Canonical evidence item used to support beliefs and relations.
/// </summary>
/// <param name="EvidenceId">Stable evidence identifier.</param>
/// <param name="SourceType">Source type, for example episode, tool, user, benchmark, or inference.</param>
/// <param name="SourceId">Stable source identifier.</param>
/// <param name="EpisodeId">Optional episode identifier linked to this evidence.</param>
/// <param name="ToolName">Optional tool name linked to this evidence.</param>
/// <param name="InputHash">Optional hash of the input that produced this evidence.</param>
/// <param name="Confidence">Normalized confidence from 0.0 to 1.0.</param>
/// <param name="ObservedAt">Timestamp when this evidence was observed.</param>
/// <param name="Metadata">Small key/value metadata for audit and routing.</param>
public sealed record WorldEvidenceContract(
    string EvidenceId,
    string SourceType,
    string SourceId,
    string? EpisodeId,
    string? ToolName,
    string? InputHash,
    double Confidence,
    DateTimeOffset ObservedAt,
    IReadOnlyDictionary<string, string> Metadata);

/// <summary>
/// Canonical belief with temporal validity, provenance and revision metadata.
/// </summary>
/// <param name="BeliefId">Stable belief identifier.</param>
/// <param name="Subject">Belief subject.</param>
/// <param name="Predicate">Belief predicate.</param>
/// <param name="Object">Belief object.</param>
/// <param name="Confidence">Normalized confidence from 0.0 to 1.0.</param>
/// <param name="ValidFrom">Optional timestamp from which the belief is valid.</param>
/// <param name="ValidUntil">Optional timestamp when the belief stops being valid.</param>
/// <param name="EvidenceIds">Evidence identifiers supporting the belief.</param>
/// <param name="ContradictionIds">Contradiction identifiers linked to the belief.</param>
/// <param name="Revision">Current revision metadata.</param>
/// <param name="Metadata">Small key/value metadata for audit and routing.</param>
public sealed record WorldBeliefContract(
    string BeliefId,
    string Subject,
    string Predicate,
    string Object,
    double Confidence,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    IReadOnlyList<string> EvidenceIds,
    IReadOnlyList<string> ContradictionIds,
    BeliefRevisionContract Revision,
    IReadOnlyDictionary<string, string> Metadata);

/// <summary>
/// Explicit contradiction between two beliefs.
/// </summary>
/// <param name="ContradictionId">Stable contradiction identifier.</param>
/// <param name="LeftBeliefId">First belief identifier.</param>
/// <param name="RightBeliefId">Second belief identifier.</param>
/// <param name="Reason">Audit-friendly contradiction reason.</param>
/// <param name="Status">Resolution status, for example open, resolved, or ignored.</param>
/// <param name="CreatedAt">Timestamp when the contradiction was created.</param>
/// <param name="ResolvedAt">Optional timestamp when the contradiction was resolved.</param>
/// <param name="Resolution">Optional resolution summary.</param>
public sealed record WorldContradictionContract(
    string ContradictionId,
    string LeftBeliefId,
    string RightBeliefId,
    string Reason,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt,
    string? Resolution);

/// <summary>
/// Audit metadata for a belief confidence revision.
/// </summary>
/// <param name="RevisionId">Stable revision identifier.</param>
/// <param name="BeliefId">Belief identifier being revised.</param>
/// <param name="PreviousConfidence">Previous confidence value.</param>
/// <param name="NewConfidence">New confidence value.</param>
/// <param name="Reason">Audit-friendly reason for the revision.</param>
/// <param name="RevisedAt">Timestamp when the revision happened.</param>
/// <param name="EvidenceEpisodeIds">Episode identifiers attached to the revision evidence.</param>
public sealed record BeliefRevisionContract(
    string RevisionId,
    string BeliefId,
    double PreviousConfidence,
    double NewConfidence,
    string Reason,
    DateTimeOffset RevisedAt,
    IReadOnlyList<string> EvidenceEpisodeIds);
