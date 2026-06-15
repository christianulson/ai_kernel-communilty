using KrnlAI.Desktop.Infrastructure.Abstractions;

namespace KrnlAI.Desktop.App.Services;

public sealed class NullAdminApi : IAdminApi
{
    public Task<List<UserInfo>> GetUsersAsync(CancellationToken ct = default) => Task.FromResult(new List<UserInfo>());
    public Task<UserInfo> GetUserAsync(string id, CancellationToken ct = default) => Task.FromException<UserInfo>(new InvalidOperationException("Admin API indisponível no modo local."));
    public Task UpdateUserStatusAsync(string id, UpdateStatusRequest request, CancellationToken ct = default) => Task.CompletedTask;
    public Task<List<ConfigEntry>> GetConfigAsync(CancellationToken ct = default) => Task.FromResult(new List<ConfigEntry>());
    public Task<List<FeatureFlag>> GetFeatureFlagsAsync(CancellationToken ct = default) => Task.FromResult(new List<FeatureFlag>());
    public Task<PrivacyRequest> RequestDataDeletionAsync(PrivacyActionRequest request, CancellationToken ct = default) => Task.FromException<PrivacyRequest>(new InvalidOperationException("Admin API indisponível no modo local."));
    public Task<PrivacyRequest> RequestDataExportAsync(PrivacyActionRequest request, CancellationToken ct = default) => Task.FromException<PrivacyRequest>(new InvalidOperationException("Admin API indisponível no modo local."));
    public Task<List<PrivacyRequest>> GetPrivacyRequestsAsync(CancellationToken ct = default) => Task.FromResult(new List<PrivacyRequest>());
}
