using Refit;

namespace Kernel.Contracts.Abstractions;

public interface IKernelCausalApi
{
    [Get("/causal/predict")]
    Task<List<CausalEdgeDto>> PredictAsync(string action, double? minConfidence = null, CancellationToken ct = default);

    [Get("/causal/causes")]
    Task<List<CausalEdgeDto>> FindCausesAsync(string outcome, double? minConfidence = null, CancellationToken ct = default);
}

public sealed record CausalEdgeDto(string FromNodeId, string ToNodeId, string Relation, double Confidence, int ObservationCount, List<string>? EvidenceEpisodeIds);
