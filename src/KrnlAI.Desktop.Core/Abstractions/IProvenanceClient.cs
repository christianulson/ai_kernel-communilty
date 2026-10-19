using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

public interface IProvenanceClient
{
    Task<List<ProvenanceEntry>> GetChainAsync(string entityId, CancellationToken ct = default);
    Task<bool> VerifyChainAsync(string entityId, CancellationToken ct = default);
}
