using KrnlAI.Embedded.Abstractions;

namespace KrnlAI.Sidecar.Rpc;

public sealed class SidecarRpcHandler
{
    private readonly IEmbeddedKrnlAI _kernel;
    private readonly ILogger<SidecarRpcHandler> _logger;

    public SidecarRpcHandler(IEmbeddedKrnlAI kernel, ILogger<SidecarRpcHandler> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<AgentRunResponse> RunAgentAsync(string prompt, CancellationToken ct = default)
    {
        var safePrompt = prompt ?? "";
        _logger.LogInformation("RPC RunAgent: prompt={PromptLen}chars", safePrompt.Length);
        var result = await _kernel.RunAsync(safePrompt, ct);
        return new AgentRunResponse
        {
            Narration = result.Narration,
            Error = result.Error,
            TransportSteps = [new TransportStepDto { Label = "EmbeddedKrnlAI", Detail = result.Mode, Ok = result.Error is null }],
            ActiveStages = ["rpc"]
        };
    }

    public Task<HealthResult> GetHealthAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new HealthResult("healthy", "rpc"));
    }

    public async Task<MemorySearchResult> SearchMemoryAsync(string query, CancellationToken ct = default)
    {
        _logger.LogInformation("RPC SearchMemory: query={Query}", query);
        var hits = await _kernel.SearchMemoryAsync(query, ct);
        return new MemorySearchResult(hits.Select(h => new MemoryHit(h.Id, h.Payload ?? "", h.Score)).ToList(), hits.Count);
    }
}

public sealed record HealthResult(string Status, string Mode);

public sealed record MemorySearchResult(List<MemoryHit> Hits, int TotalCount);

public sealed record MemoryHit(string Id, string Content, double Score);
