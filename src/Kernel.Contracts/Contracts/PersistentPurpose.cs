namespace Kernel.Contracts;

public sealed record PersistentPurpose(
    string PurposeId,
    string Statement,
    string Scope,
    string Status,
    string Priority,
    IReadOnlyList<string> Principles,
    IReadOnlyList<string> Guardrails,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReviewAfter);
