using System.Text.Json.Serialization;

namespace KrnlAI.VisualStudio.Services;

public sealed record KanbanResponse(
    IReadOnlyList<KanbanColumn> Columns,
    KanbanMetadata Metadata);

public sealed record KanbanColumn(
    string Column,
    string Label,
    IReadOnlyList<KanbanCard> Cards,
    int TotalCount);

public sealed record KanbanCard(
    string Id,
    string Description,
    string Status,
    double Progress,
    double Priority,
    string? Domain,
    DateTimeOffset CreatedAt,
    DateTimeOffset? Deadline,
    string? ParentGoalId,
    IReadOnlyList<KanbanCard>? SubGoals);

public sealed record KanbanMetadata(
    int TotalGoals,
    int TotalColumns,
    KanbanFilter Filters);

public sealed record KanbanFilter(
    int DaysBack,
    string? Domain,
    double? MinPriority,
    string? UserId,
    string? Search);

public interface IKanbanService
{
    Task<KanbanResponse?> GetKanbanAsync(
        int daysBack = 10,
        string? domain = null,
        double? minPriority = null,
        string? search = null,
        CancellationToken ct = default);

    Task<bool> MoveCardAsync(string cardId, string newStatus, CancellationToken ct = default);
}
