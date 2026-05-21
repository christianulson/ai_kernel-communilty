using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

public interface IAdminClient
{
    Task<UserProfile?> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateUserProfileAsync(UserProfile profile, CancellationToken cancellationToken = default);
    Task<ShareListResponse?> GetSharesAsync(CancellationToken cancellationToken = default);
}
