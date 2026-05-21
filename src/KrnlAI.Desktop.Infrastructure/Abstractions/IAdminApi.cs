using Refit;

namespace KrnlAI.Desktop.Infrastructure.Abstractions;

public sealed record UserInfo(string Id, string Name, string Email, string Role, bool IsActive, DateTime CreatedAt);
public sealed record ConfigEntry(string Key, string Value, string Type);
public sealed record FeatureFlag(string Name, bool Enabled, string Category);
public sealed record PrivacyRequest(string RequestId, string Type, string Status, DateTime CreatedAt);

public interface IAdminApi
{
    [Get("/admin/users")]
    Task<List<UserInfo>> GetUsersAsync(CancellationToken ct = default);

    [Get("/admin/users/{id}")]
    Task<UserInfo> GetUserAsync(string id, CancellationToken ct = default);

    [Post("/admin/users/{id}/status")]
    Task UpdateUserStatusAsync(string id, [Body] UpdateStatusRequest request, CancellationToken ct = default);

    [Get("/admin/config")]
    Task<List<ConfigEntry>> GetConfigAsync(CancellationToken ct = default);

    [Get("/admin/feature-flags")]
    Task<List<FeatureFlag>> GetFeatureFlagsAsync(CancellationToken ct = default);

    [Post("/privacy/delete")]
    Task<PrivacyRequest> RequestDataDeletionAsync([Body] PrivacyActionRequest request, CancellationToken ct = default);

    [Post("/privacy/export")]
    Task<PrivacyRequest> RequestDataExportAsync([Body] PrivacyActionRequest request, CancellationToken ct = default);

    [Get("/privacy/requests")]
    Task<List<PrivacyRequest>> GetPrivacyRequestsAsync(CancellationToken ct = default);
}

public sealed record UpdateStatusRequest(bool IsActive);
public sealed record PrivacyActionRequest(string Reason);
