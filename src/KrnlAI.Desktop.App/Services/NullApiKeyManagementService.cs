using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.Services;

/// <summary>
/// Local-mode fallback used when the desktop app is not connected to the auth backend.
/// </summary>
public sealed class NullApiKeyManagementService : IApiKeyManagementService
{
    public Task<IReadOnlyList<ApiKeyListItem>> ListAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ApiKeyListItem>>([]);

    public Task<ApiKeyCreationResult> CreateAsync(ApiKeyCreationRequest request, CancellationToken ct = default)
        => Task.FromException<ApiKeyCreationResult>(new InvalidOperationException("API keys indisponíveis no modo local."));

    public Task RevokeAsync(string keyId, CancellationToken ct = default)
        => Task.FromException(new InvalidOperationException("API keys indisponíveis no modo local."));

    public Task<ApiKeyUsageSummary> GetStatsAsync(CancellationToken ct = default)
        => Task.FromResult(new ApiKeyUsageSummary(0, 0, 0, 0, null));
}
