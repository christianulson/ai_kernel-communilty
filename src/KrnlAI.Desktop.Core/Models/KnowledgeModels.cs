namespace KrnlAI.Desktop.Core.Models;

public sealed record KnowledgeQueryResult(string Query, List<KnowledgeHit> Hits, int TotalCount);

public sealed record KnowledgeHit(string Id, string Content, double Score, string? Source, DateTimeOffset CreatedAt);

public sealed record KnowledgeStats(int TotalEntries, int TotalSources, int QueriesToday, DateTimeOffset LastIndexed);

public sealed record KnowledgeLearnRequest(string Content, string Source, string? Category);

public sealed record KnowledgeLearnResponse(bool Success, string? EntryId, string? Error);
