using Refit;

namespace Kernel.Contracts.Abstractions;

public interface IKernelProfileApi
{
    [Get("/profile/{userId}")]
    Task<UserProfileDto> GetAsync(string userId, CancellationToken ct);

    [Post("/profile")]
    Task<bool> UpsertAsync([Body] UpsertProfileRequest request, CancellationToken ct);

    [Get("/profile/emotional")]
    Task<EmotionalStateDto> GetEmotionalAsync(string userId, CancellationToken ct);
}

public sealed record UserProfileDto(string UserId, string? DisplayName, string? PreferencesJson);
public sealed record UpsertProfileRequest(string UserId, string? DisplayName, string? PreferencesJson);
public sealed record EmotionalStateDto(string DominantEmotion, double Intensity, double Valence, DateTimeOffset Timestamp);
