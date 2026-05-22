namespace KrnlAI.Desktop.Core.Models;

public sealed record KanbanDisplay(
    IReadOnlyList<KanbanColumnDisplay> Columns,
    KanbanMetadataDisplay Metadata);

public sealed record KanbanColumnDisplay(
    string ColumnKey,
    string Label,
    IReadOnlyList<KanbanCardDisplay> Cards,
    int TotalCount);

public sealed record KanbanCardDisplay(
    string Id,
    string Description,
    string Status,
    double Progress,
    double Priority,
    string? Domain,
    DateTimeOffset CreatedAt,
    DateTimeOffset? Deadline,
    string? ParentGoalId,
    IReadOnlyList<KanbanCardDisplay>? SubGoals);

public sealed record KanbanMetadataDisplay(
    int TotalGoals,
    int TotalColumns,
    KanbanFilterDisplay Filters);

public sealed record KanbanFilterDisplay(
    int DaysBack,
    string? Domain,
    double? MinPriority,
    string? UserId,
    string? Search);
