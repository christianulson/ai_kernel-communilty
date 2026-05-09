namespace Kernel.Contracts;

public sealed record AgentInstance(
    string InstanceId,
    string RoleName,
    string DisplayName,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastActiveAt,
    int TotalTasksAssigned,
    int TasksCompleted,
    double SuccessRate,
    IReadOnlyList<string> CurrentTaskIds,
    IReadOnlyDictionary<string, double> PerformanceMetrics,
    IReadOnlyList<string>? Capabilities = null,
    IReadOnlyList<string>? AllowedTools = null,
    string? DefaultMemoryScope = null,
    string? MaxRiskLevel = null);

public sealed record AgentTaskAssignment(
    string AssignmentId,
    string AgentInstanceId,
    string TaskId,
    string TaskDescription,
    string Status,
    DateTimeOffset AssignedAt,
    DateTimeOffset? CompletedAt,
    string? ResultSummary);

public sealed record AgentCollaboration(
    string CollaborationId,
    string PrimaryAgentId,
    IReadOnlyList<string> SupportingAgentIds,
    string Goal,
    string Strategy,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    bool Success);
