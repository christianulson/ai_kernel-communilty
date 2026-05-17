namespace Kernel.Contracts;

public sealed record PurposeAlignmentResult(
    string ObjectiveId,
    string? PurposeId,
    double AlignmentScore,
    IReadOnlyList<string> MatchingPrinciples,
    IReadOnlyList<string> ViolatedGuardrails,
    bool RequiresApproval);
