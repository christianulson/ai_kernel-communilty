using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiKernel.Sdk.Models;

public sealed record MetricEntry(
    [property: JsonPropertyName("totalRequests")] int? TotalRequests = null,
    [property: JsonPropertyName("totalFallbacks")] int? TotalFallbacks = null,
    [property: JsonPropertyName("avgCandidates")] double? AvgCandidates = null,
    [property: JsonPropertyName("avgHits")] double? AvgHits = null,
    [property: JsonPropertyName("updatedAt")] string? UpdatedAt = null,
    [property: JsonPropertyName("modeCounts")] IReadOnlyDictionary<string, int>? ModeCounts = null,
    [property: JsonPropertyName("candidateSourceCounts")] IReadOnlyDictionary<string, int>? CandidateSourceCounts = null,
    [property: JsonPropertyName("fallbackReasonCounts")] IReadOnlyDictionary<string, int>? FallbackReasonCounts = null
);

public sealed record MetricsSummary(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("metrics")] MetricEntry Metrics
);
