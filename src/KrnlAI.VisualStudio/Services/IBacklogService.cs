namespace KrnlAI.VisualStudio.Services;

public sealed record BacklogItem(
    string Id,
    string Title,
    string Description,
    string Status,
    string Priority,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public interface IBacklogService
{
    Task<IReadOnlyList<BacklogItem>?> GetItemsAsync(
        string? status = null,
        CancellationToken ct = default);

    Task<BacklogItem?> CreateItemAsync(
        string title,
        string? description,
        string priority,
        IReadOnlyList<string>? tags,
        CancellationToken ct = default);

    Task<bool> MoveItemAsync(
        string id,
        string newStatus,
        CancellationToken ct = default);

    Task<bool> DeleteItemAsync(
        string id,
        CancellationToken ct = default);
}
