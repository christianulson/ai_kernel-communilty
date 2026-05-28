namespace KrnlAI.Desktop.Core.Models;

public enum ApiKeyScope
{
    ReadOnly = 0,
    ReadWrite = 1,
    Full = 2
}

public sealed record ApiKeyListItem(
    string KeyId,
    string KeyPrefix,
    string Name,
    ApiKeyScope Scope,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? LastUsedAt,
    bool Active)
{
    public string DisplayPrefix => string.IsNullOrWhiteSpace(KeyPrefix)
        ? "krnl_••••"
        : $"{KeyPrefix[..Math.Min(KeyPrefix.Length, 13)]}••••";

    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;
}

public sealed record ApiKeyCreationRequest(string Name, TimeSpan? Ttl, ApiKeyScope Scope);

public sealed record ApiKeyCreationResult(
    string KeyId,
    string FullKey,
    string Name,
    ApiKeyScope Scope,
    DateTimeOffset ExpiresAt,
    string Warning);

public sealed record ApiKeyUsageSummary(
    int Total,
    int Active,
    int Expired,
    int Revoked,
    [property: System.Text.Json.Serialization.JsonPropertyName("lastUsed")] DateTimeOffset? LastUsedAt);
