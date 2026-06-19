namespace KrnlAI.Desktop.Core.Models;

public record EpisodicMemorySearchRequest(
    string Query,
    int TopK = 10,
    string? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
);

public record EpisodicMemorySearchResult(
    List<EpisodicMemoryHit> Hits,
    int TotalCount,
    string Query
);

public record EpisodicMemoryHit(
    string EpisodeId,
    string Goal,
    string Summary,
    string Status,
    double? Similarity,
    DateTimeOffset CreatedAt
);
