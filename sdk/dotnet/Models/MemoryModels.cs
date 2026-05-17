using System.Text.Json.Serialization;

namespace AiKernel.Sdk.Models;

public sealed record MemorySearchRequest(
    [property: JsonPropertyName("query")] string Query,
    [property: JsonPropertyName("topK")] int TopK = 5
);

public sealed record MemorySearchHit(
    [property: JsonPropertyName("score")] double Score,
    [property: JsonPropertyName("docId")] string DocId,
    [property: JsonPropertyName("chunkId")] string ChunkId,
    [property: JsonPropertyName("text")] string Text
);

public sealed record MemorySearchResponse(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("hits")] IReadOnlyList<MemorySearchHit> Hits
);

public sealed record MemoryIngestRequest(
    [property: JsonPropertyName("docId")] string DocId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("source")] string? Source = null,
    [property: JsonPropertyName("tagsJson")] string? TagsJson = null
);

public sealed record MemoryIngestResponse(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("docId")] string DocId,
    [property: JsonPropertyName("chunkCount")] int ChunkCount
);
