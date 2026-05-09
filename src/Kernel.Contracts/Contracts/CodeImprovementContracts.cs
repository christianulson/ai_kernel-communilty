namespace Kernel.Contracts;

/// <summary>
/// Patch step proposed by the controlled code-improvement loop.
/// </summary>
/// <param name="FilePath">Target file path for the patch step.</param>
/// <param name="ChangeSummary">Short summary of the intended change.</param>
/// <param name="Rationale">Why this step is needed.</param>
/// <param name="ApplyDirectlyToProduction">Whether this step can be applied directly to production.</param>
public sealed record CodeImprovementPatchStep(
    string FilePath,
    string ChangeSummary,
    string Rationale,
    bool ApplyDirectlyToProduction);

/// <summary>
/// Proposal for a bounded code improvement plan.
/// </summary>
/// <param name="ProposalId">Stable proposal identifier.</param>
/// <param name="CandidateId">Source candidate identifier.</param>
/// <param name="ProposalKind">Kind of improvement being proposed.</param>
/// <param name="TargetMetric">Metric the proposal aims to improve.</param>
/// <param name="ProposedChange">Batched textual summary of the change.</param>
/// <param name="PatchPlan">Ordered patch steps for review.</param>
/// <param name="ApplyToProduction">Whether the proposal is allowed to apply directly to production.</param>
/// <param name="RequiresHumanApproval">Whether human approval is mandatory.</param>
public sealed record CodeImprovementProposal(
    string ProposalId,
    string CandidateId,
    string ProposalKind,
    string TargetMetric,
    string ProposedChange,
    IReadOnlyList<CodeImprovementPatchStep> PatchPlan,
    bool ApplyToProduction,
    bool RequiresHumanApproval);
