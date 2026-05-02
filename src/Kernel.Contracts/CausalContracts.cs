namespace Kernel.Contracts;

public record CausalNodeContract(
    string Id,
    string Type,
    string Description,
    IReadOnlyDictionary<string, string> Metadata
);

public record CausalEdgeContract(
    string SourceId,
    string TargetId,
    string Relation,
    double Confidence,
    int EvidenceCount
);

public record EmotionalStateContract(
    double Valence,
    double Arousal,
    double Motivation,
    DateTimeOffset UpdatedAt
);
