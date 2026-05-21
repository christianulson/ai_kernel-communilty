using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

public interface IMemoryClient
{
    Task<MemorySearchResult> SearchMemoryAsync(string query, int topK = 10, CancellationToken cancellationToken = default);
    Task<MemoryIngestResult> IngestMemoryAsync(MemoryIngestRequest request, CancellationToken cancellationToken = default);
    Task<MemoryMetrics?> GetMemoryMetricsAsync(CancellationToken cancellationToken = default);
    Task<WorkingMemorySummary?> GetWorkingMemoryAsync(CancellationToken cancellationToken = default);
}
