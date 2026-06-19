using KrnlAI.Contracts.Contracts;
using KrnlAI.Embedded.Abstractions;

namespace KrnlAI.Sidecar.Rpc;

public sealed class SidecarRpcHandler(IEmbeddedKrnlAI kernel, ILogger<SidecarRpcHandler> logger)
{
    public async Task<AgentRunTransportResponse> RunAgentAsync(string prompt, CancellationToken ct = default)
    {
        var safePrompt = prompt ?? "";
        logger.LogInformation("RPC RunAgent: prompt={PromptLen}chars", safePrompt.Length);
        var result = await kernel.RunAsync(safePrompt, ct);
        return new AgentRunTransportResponse(
            Narration: result.Narration,
            Command: null,
            TransportSteps: [new TransportStepDto("EmbeddedKrnlAI", result.Mode, result.Error is null, null)],
            ActiveStages: ["rpc"],
            Error: result.Error
        );
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
