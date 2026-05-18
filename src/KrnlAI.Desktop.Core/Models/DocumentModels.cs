namespace KrnlAI.Desktop.Core.Models;

public record DocumentInfo(
    string DocumentId, string FileName, long FileSize, string Format,
    string Status, string? ErrorMessage, int ChunkCount,
    DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt);

public record DocumentSearchResult(
    IReadOnlyList<DocumentInfo> Documents);

public record DocumentSearchHit(
    string ChunkId, string DocId, string Text, double Score);

public record DocumentSearchResponse(
    bool Ok, IReadOnlyList<DocumentSearchHit> Hits);
