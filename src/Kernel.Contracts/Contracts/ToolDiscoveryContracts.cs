namespace Kernel.Contracts;

public sealed record DiscoveredTool(
    string Name,
    string Description,
    IReadOnlyList<string> RequiredTools,
    string CompositionStrategy,
    double EstimatedValue,
    int UsageCount);

public sealed record ToolChainStep(
    string ToolName,
    object InputTemplate,
    string OutputKey);

public sealed record ToolChainDefinition(
    string Name,
    string Description,
    IReadOnlyList<ToolChainStep> Steps,
    string RiskLevel);

public sealed record ToolDiscoveryResult(
    IReadOnlyList<DiscoveredTool> Discovered,
    DateTimeOffset DiscoveredAt);
