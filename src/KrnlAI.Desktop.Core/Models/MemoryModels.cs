namespace KrnlAI.Desktop.Core.Models;

public record MemorySearchResult(
    List<MemoryHit> Hits,
    int TotalCount,
    double QueryTimeMs
);

public record MemoryHit(
    string Id,
    string Content,
    string Source,
    double Score,
    DateTime CreatedAt,
    Dictionary<string, string>? Metadata
);

public record MemoryIngestRequest(
    string Content,
    string? Source = null,
    string? Type = null,
    Dictionary<string, string>? Metadata = null
);

public record MemoryMetrics(
    int TotalChunks,
    int TotalDocuments,
    long TotalSizeBytes,
    Dictionary<string, int> BySource,
    DateTime? OldestEntry,
    DateTime? NewestEntry
);

public record WorkingMemorySummary(
    int ActiveSlots,
    int MaxSlots,
    List<WorkingMemorySlot> Slots
);

public record WorkingMemorySlot(
    string Key,
    string Content,
    double Relevance,
    DateTime CreatedAt,
    DateTime? ExpiresAt
);

public record MemoryIngestResult(
    bool Success,
    string? DocumentId,
    int ChunksCreated,
    string? Error
);