using System.Globalization;
using System.Net.Http.Json;
using KrnlAI.Contracts;
using KrnlAI.LLMGateway.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Cli.Services;

/// <summary>HTTP client implementation of IKanbanService that calls the Kernel API.</summary>
public sealed class KanbanService(
    HttpClient http,
    ILogger<KanbanService>? logger = null) : IKanbanService
{
    public async Task<KanbanResponse> GetKanbanAsync(
        int daysBack = 10,
        string? domain = null,
        double? minPriority = null,
        string? userId = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var url = $"/api/goals/kanban?daysBack={daysBack}";
        if (domain is not null) url += $"&domain={Uri.EscapeDataString(domain)}";
        if (minPriority.HasValue) url += $"&minPriority={minPriority.Value.ToString(CultureInfo.InvariantCulture)}";
        if (userId is not null) url += $"&userId={Uri.EscapeDataString(userId)}";
        if (search is not null) url += $"&search={Uri.EscapeDataString(search)}";

        logger?.LogDebug("Calling Kanban API: {Url}", url);
        var resp = await http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<KanbanResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Failed to deserialize KanbanResponse");
    }
}
