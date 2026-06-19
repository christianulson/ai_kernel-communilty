namespace KrnlAI.Desktop.Core.Models;

public record FlowNode(
    string Id,
    string StepType,
    string Label,
    double X,
    double Y,
    bool Enabled,
    int Timeout,
    string? SkipCondition);

public record FlowEdge(
    string SourceId,
    string TargetId);

public record FlowDefinition(
    string Name,
    string Description,
    List<FlowNode> Nodes,
    List<FlowEdge> Edges);

public record CognitiveFlowResult(
    bool Success,
    string? Output,
    string? Error);

public record CognitiveFlowList(
    List<FlowDefinition> Flows);
