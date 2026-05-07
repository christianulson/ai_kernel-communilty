namespace Kernel.Contracts;

/// <summary>
/// Structured hierarchical plan with dependency graph, rollback intent and success signals.
/// </summary>
/// <param name="Goal">Global goal for the plan.</param>
/// <param name="Steps">Ordered execution steps.</param>
/// <param name="Graph">Dependency graph derived from the steps.</param>
/// <param name="SuccessCriteria">Success conditions for the overall plan.</param>
/// <param name="ReplanSignals">Signals that should trigger replanning.</param>
/// <param name="RollbackPlan">Rollback strategy for mutable actions.</param>
/// <param name="Metadata">Auxiliary metadata for governance and audit.</param>
public sealed record StructuredPlan(
    string Goal,
    IReadOnlyList<StructuredPlanStep> Steps,
    PlanGraph Graph,
    IReadOnlyList<string> SuccessCriteria,
    IReadOnlyList<string> ReplanSignals,
    string? RollbackPlan,
    IReadOnlyDictionary<string, string> Metadata)
{
    public static StructuredPlan Create(
        string Goal,
        IReadOnlyList<StructuredPlanStep> Steps,
        IReadOnlyList<string>? SuccessCriteria = null,
        IReadOnlyList<string>? ReplanSignals = null,
        string? RollbackPlan = null,
        IReadOnlyDictionary<string, string>? Metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Goal);
        ArgumentNullException.ThrowIfNull(Steps);

        if (Steps.Count == 0)
            throw new ArgumentException("Structured plan requires at least one step.", nameof(Steps));

        ValidateSteps(Steps);

        var graph = PlanGraph.FromSteps(Steps);

        return new StructuredPlan(
            Goal.Trim(),
            [.. Steps],
            graph,
            SuccessCriteria is { Count: > 0 } ? [.. SuccessCriteria] : [],
            ReplanSignals is { Count: > 0 } ? [.. ReplanSignals] : [],
            string.IsNullOrWhiteSpace(RollbackPlan) ? null : RollbackPlan.Trim(),
            Metadata is not null ? new Dictionary<string, string>(Metadata, StringComparer.Ordinal) : new Dictionary<string, string>(StringComparer.Ordinal));
    }

    private static void ValidateSteps(IReadOnlyList<StructuredPlanStep> steps)
    {
        var seenStepIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var step in steps)
        {
            if (step is null)
                throw new ArgumentException("Structured plan contains a null step.", nameof(steps));

            if (string.IsNullOrWhiteSpace(step.StepId))
                throw new ArgumentException("Structured plan step requires StepId.", nameof(steps));

            if (!seenStepIds.Add(step.StepId.Trim()))
                throw new ArgumentException($"Structured plan contains duplicate step id '{step.StepId}'.", nameof(steps));

            if (step.Preconditions is null || step.Preconditions.Count == 0)
                throw new ArgumentException($"Structured plan step '{step.StepId}' must declare at least one precondition.", nameof(steps));
        }
    }
}

/// <summary>
/// Structured step in a hierarchical plan.
/// </summary>
/// <param name="StepId">Stable step identifier.</param>
/// <param name="Tool">Tool or capability to execute.</param>
/// <param name="Input">Input payload for the step.</param>
/// <param name="ExpectedOutcome">Expected step outcome.</param>
/// <param name="Preconditions">Step identifiers or checkpoints that must be satisfied before execution.</param>
/// <param name="Effects">Expected effects after successful execution.</param>
/// <param name="Risk">Risk level associated with the step.</param>
/// <param name="RollbackHint">Rollback hint for the step.</param>
/// <param name="AllowedTools">Allowlist of tools for the step.</param>
/// <param name="EstimatedCost">Estimated cost of the step.</param>
public sealed record StructuredPlanStep(
    string StepId,
    string Tool,
    object Input,
    string ExpectedOutcome,
    IReadOnlyList<string> Preconditions,
    IReadOnlyList<string> Effects,
    string Risk,
    string? RollbackHint,
    IReadOnlyList<string> AllowedTools,
    decimal EstimatedCost = 0m);

/// <summary>
/// Dependency graph associated with a structured plan.
/// </summary>
/// <param name="Nodes">Graph nodes representing steps or checkpoints.</param>
/// <param name="Edges">Directed dependencies between nodes.</param>
public sealed record PlanGraph(
    IReadOnlyList<PlanGraphNode> Nodes,
    IReadOnlyList<PlanGraphEdge> Edges)
{
    public static PlanGraph FromSteps(IReadOnlyList<StructuredPlanStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        var nodes = steps
            .Select(step => new PlanGraphNode(step.StepId, step.Tool, step.ExpectedOutcome))
            .ToList();

        var edges = new List<PlanGraphEdge>();
        foreach (var step in steps)
        {
            foreach (var precondition in step.Preconditions)
            {
                if (string.IsNullOrWhiteSpace(precondition))
                    continue;

                edges.Add(new PlanGraphEdge(precondition.Trim(), step.StepId, "precondition"));
            }
        }

        return new PlanGraph(nodes, edges);
    }
}

/// <summary>
/// Node in a structured plan graph.
/// </summary>
/// <param name="NodeId">Stable node identifier.</param>
/// <param name="Label">Human-readable label.</param>
/// <param name="Description">Optional description or outcome summary.</param>
public sealed record PlanGraphNode(string NodeId, string Label, string Description);

/// <summary>
/// Directed edge in a structured plan graph.
/// </summary>
/// <param name="FromNodeId">Origin node identifier.</param>
/// <param name="ToNodeId">Destination node identifier.</param>
/// <param name="Relation">Type of dependency relation.</param>
public sealed record PlanGraphEdge(string FromNodeId, string ToNodeId, string Relation);
