using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

public interface IAuthClient
{
    void SetAuthToken(string? token);
    void SetTokens(string? token, string? refreshToken);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
