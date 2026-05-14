using Refit;

namespace Kernel.Contracts.Abstractions;

public interface IKernelMemoryApi
{
    [Post("/memory/search")]
    Task<MemorySearchResponse> SearchAsync([Body] SearchMemoryRequest request, CancellationToken ct);

    [Post("/memory/upsert")]
    Task<UpsertResponse> UpsertAsync([Body] UpsertDocumentRequest request, CancellationToken ct);

    [Get("/memory/metrics/summary")]
    Task<MemoryMetricsResponse> GetMetricsAsync(CancellationToken ct);

    [Get("/memory/working-context")]
    Task<WorkingContextResponse> GetWorkingContextAsync(int maxLines, CancellationToken ct);

    [Post("/episode-memory/search")]
    Task<EpisodeMemorySearchResponse> SearchEpisodeMemoryAsync([Body] SearchEpisodeMemoryRequest request, CancellationToken ct);

    [Post("/memory/multimodal/ingest")]
    Task<MultimodalIngestResponse> IngestMultimodalAsync([Body] MultimodalInputDto request, CancellationToken ct);

    [Post("/memory/multimodal/search")]
    Task<MultimodalSearchResponse> SearchMultimodalAsync([Body] MultimodalSearchDto request, CancellationToken ct);
}

public sealed record SearchMemoryRequest(string Query, int TopK);
public sealed record MemorySearchResponse(bool Ok, List<MemoryHit> Hits);
public sealed record MemoryHit(double Score, string DocId, string ChunkId, string Text, DateTimeOffset? CreatedAt = null);
public sealed record UpsertDocumentRequest(string DocId, string Title, string? Source, string? TagsJson, string Text);
public sealed record UpsertResponse(bool Ok, string DocId, int ChunkCount);
public sealed record MemoryMetricsResponse(bool Ok, MemoryMetricsValue Metrics);
public sealed record MemoryMetricsValue(int TotalRequests, int TotalFallbacks, int AvgCandidates, int AvgHits, DateTimeOffset UpdatedAt);
public sealed record WorkingContextResponse(bool Ok, List<WorkingMemoryLine> Lines);
public sealed record WorkingMemoryLine(string Key, string Content, double Relevance, DateTimeOffset CreatedAt, DateTimeOffset? ExpiresAt);
public sealed record SearchEpisodeMemoryRequest(string UserId, string Query, int TopK = 5);
public sealed record EpisodeMemorySearchResponse(bool Ok, List<EpisodeMemoryHit> Hits);
public sealed record EpisodeMemoryHit(string Text, double Score, string? DocId = null, string? ChunkId = null, DateTimeOffset? CreatedAt = null);
public sealed record MultimodalInputDto(string DataType, string Content, string? BinaryDataBase64 = null, string? MetadataJson = null);
public sealed record MultimodalIngestResponse(string ChunkId, double Confidence, string ExtractedText, DateTimeOffset IngestedAt);
public sealed record MultimodalSearchDto(string Query, int TopK = 10, string? FilterType = null);
public sealed record MultimodalSearchResponse(int Count, List<MultimodalSearchHit> Results);
public sealed record MultimodalSearchHit(string ChunkId, string SourceId, string DataType, string Content, double Score);
