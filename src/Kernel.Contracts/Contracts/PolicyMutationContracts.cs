namespace Kernel.Contracts;

/// <summary>
/// Proposal for a policy mutation produced by the self-improvement loop.
/// </summary>
/// <param name="ProposalId">Stable proposal identifier.</param>
/// <param name="PolicyName">Policy key being mutated.</param>
/// <param name="CurrentValue">Current value before mutation.</param>
/// <param name="ProposedValue">Proposed new value.</param>
/// <param name="ImpactScore">Normalized impact score from 0 to 1.</param>
/// <param name="RiskLevel">Expected risk level.</param>
/// <param name="Evidence">Evidence ids supporting the proposal.</param>
public sealed record PolicyMutationProposal(
    string ProposalId,
    string PolicyName,
    string CurrentValue,
    string ProposedValue,
    double ImpactScore,
    string RiskLevel,
    IReadOnlyList<string> Evidence);

/// <summary>
/// Result of evaluating a policy mutation proposal.
/// </summary>
/// <param name="Approved">Whether the proposal is approved.</param>
/// <param name="RequiresHumanApproval">Whether human approval is mandatory.</param>
/// <param name="RejectionReason">Reason for rejection, if any.</param>
/// <param name="PolicyName">Policy name under evaluation.</param>
/// <param name="ImpactScore">Impact score of the proposal.</param>
public sealed record PolicyMutationDecision(
    bool Approved,
    bool RequiresHumanApproval,
    string? RejectionReason,
    string PolicyName,
    double ImpactScore);
