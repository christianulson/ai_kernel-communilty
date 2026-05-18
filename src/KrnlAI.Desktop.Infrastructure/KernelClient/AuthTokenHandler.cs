using System.Net.Http.Headers;

namespace KrnlAI.Desktop.Infrastructure.KernelClient;

public sealed class AuthTokenHandler(AuthTokenProvider tokenProvider) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(tokenProvider.Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenProvider.Token);
        return base.SendAsync(request, ct);
    }
}
