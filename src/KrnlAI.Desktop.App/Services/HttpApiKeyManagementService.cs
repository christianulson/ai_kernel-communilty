using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.Services;

/// <summary>
/// HTTP implementation of the desktop API key management surface.
/// </summary>
public sealed class HttpApiKeyManagementService(HttpClient httpClient) : IApiKeyManagementService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<ApiKeyListItem>> ListAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("/account/api-keys", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ApiKeyListItem>>(JsonOptions, ct).ConfigureAwait(false)
            ?? [];
    }

    public async Task<ApiKeyCreationResult> CreateAsync(ApiKeyCreationRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/account/api-keys", request, JsonOptions, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ApiKeyCreationResult>(JsonOptions, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("API key creation response was empty.");
    }

    public async Task RevokeAsync(string keyId, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync($"/account/api-keys/{keyId}/revoke", content: null, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task<ApiKeyUsageSummary> GetStatsAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("/account/api-keys/stats", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ApiKeyUsageSummary>(JsonOptions, ct).ConfigureAwait(false)
            ?? new ApiKeyUsageSummary(0, 0, 0, 0, null);
    }
}
