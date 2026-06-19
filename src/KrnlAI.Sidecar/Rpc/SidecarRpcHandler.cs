using KrnlAI.Embedded.Abstractions;

namespace KrnlAI.Sidecar.Rpc;

public sealed class SidecarRpcHandler(IEmbeddedKrnlAI kernel, ILogger<SidecarRpcHandler> logger)
{
    public async Task<AgentRunResponse> RunAgentAsync(string prompt, CancellationToken ct = default)
    {
        var safePrompt = prompt ?? "";
        logger.LogInformation("RPC RunAgent: prompt={PromptLen}chars", safePrompt.Length);
        var result = await kernel.RunAsync(safePrompt, ct);
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
        logger.LogInformation("RPC SearchMemory: query={Query}", query);
        var hits = await kernel.SearchMemoryAsync(query, ct);
        return new MemorySearchResult([.. hits.Select(h => new MemoryHit(h.Id, h.Payload ?? "", h.Score))], hits.Count);
    }
}

public sealed record HealthResult(string Status, string Mode);

public sealed record MemorySearchResult(List<MemoryHit> Hits, int TotalCount);

public sealed record MemoryHit(string Id, string Content, double Score);
