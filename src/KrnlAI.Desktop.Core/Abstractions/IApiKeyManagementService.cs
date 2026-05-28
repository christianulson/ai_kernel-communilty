using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

/// <summary>
/// Manages self-service API keys for the signed-in desktop user.
/// </summary>
public interface IApiKeyManagementService
{
    Task<IReadOnlyList<ApiKeyListItem>> ListAsync(CancellationToken ct = default);
    Task<ApiKeyCreationResult> CreateAsync(ApiKeyCreationRequest request, CancellationToken ct = default);
    Task RevokeAsync(string keyId, CancellationToken ct = default);
    Task<ApiKeyUsageSummary> GetStatsAsync(CancellationToken ct = default);
}
