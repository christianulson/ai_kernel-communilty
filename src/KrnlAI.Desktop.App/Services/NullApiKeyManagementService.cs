using System.Collections.Concurrent;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.Services;

/// <summary>
/// Local-mode API key management with in-memory storage.
/// Allows creating, listing, and revoking API keys even when offline.
/// </summary>
public sealed class NullApiKeyManagementService : IApiKeyManagementService
{
    private readonly ConcurrentDictionary<string, ApiKeyItem> _keys = new();

    public NullApiKeyManagementService()
    {
        var now = DateTimeOffset.UtcNow;
        _keys["local-default"] = new ApiKeyItem("local-default", "krnl_local_default", "Default Local Key", ApiKeyScope.Full, now, now.AddYears(1), now, true);
    }

    public Task<IReadOnlyList<ApiKeyListItem>> ListAsync(CancellationToken ct = default)
    {
        var items = _keys.Values.Select(k => new ApiKeyListItem(k.Id, k.KeyPrefix, k.Name, k.Scope, k.CreatedAt, k.ExpiresAt, k.LastUsedAt, k.IsActive)).ToList();
        return Task.FromResult<IReadOnlyList<ApiKeyListItem>>(items);
    }

    public Task<ApiKeyCreationResult> CreateAsync(ApiKeyCreationRequest request, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString("N");
        var prefix = $"krnl_local_{id[..8]}";
        var fullKey = $"{prefix}_{Guid.NewGuid().ToString("N")}";
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(Math.Max(1, request.Ttl?.Days ?? 30));
        _keys[id] = new ApiKeyItem(id, prefix, request.Name, request.Scope, now, expiresAt, null, true);
        return Task.FromResult(new ApiKeyCreationResult(id, fullKey, request.Name, request.Scope, expiresAt, "Chave criada localmente. Copie o valor agora."));
    }

    public Task RevokeAsync(string keyId, CancellationToken ct = default)
    {
        if (_keys.TryGetValue(keyId, out var existing))
            _keys[keyId] = existing with { IsActive = false };
        return Task.CompletedTask;
    }

    public Task<ApiKeyUsageSummary> GetStatsAsync(CancellationToken ct = default)
    {
        var active = _keys.Values.Count(k => k.IsActive);
        var total = _keys.Count;
        return Task.FromResult(new ApiKeyUsageSummary(total, active, 0, 0, null));
    }

    private sealed record ApiKeyItem(string Id, string KeyPrefix, string Name, ApiKeyScope Scope, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, DateTimeOffset? LastUsedAt, bool IsActive);
}
