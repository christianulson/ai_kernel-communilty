using System.Net;
using System.Net.Http.Headers;

namespace KrnlAI.Desktop.Infrastructure.KernelClient;

public sealed class AuthTokenHandler(AuthTokenProvider tokenProvider, Func<CancellationToken, Task<string?>>? refreshHandler = null) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(tokenProvider.Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenProvider.Token);

        var response = await base.SendAsync(request, ct).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.Unauthorized
            && tokenProvider.RefreshToken != null
            && refreshHandler != null
            && !IsRefreshRequest(request))
        {
            var newToken = await refreshHandler(ct).ConfigureAwait(false);
            if (newToken != null)
            {
                tokenProvider.Token = newToken;
                var retry = await CloneRequestAsync(request, ct).ConfigureAwait(false);
                retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                response.Dispose();
                return await base.SendAsync(retry, ct).ConfigureAwait(false);
            }
            tokenProvider.Clear();
        }

        return response;
    }

    private static bool IsRefreshRequest(HttpRequestMessage request) =>
        request.RequestUri?.AbsolutePath?.EndsWith("/auth/refresh", StringComparison.OrdinalIgnoreCase) == true
        || request.RequestUri?.AbsolutePath?.EndsWith("/auth/oauth2/callback", StringComparison.OrdinalIgnoreCase) == true;

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        if (request.Content != null)
        {
            var body = await request.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
            clone.Content = new ByteArrayContent(body);
            if (request.Content.Headers.ContentType != null)
                clone.Content.Headers.ContentType = request.Content.Headers.ContentType;
        }
        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        foreach (var option in request.Options)
            clone.Options.TryAdd(option.Key, option.Value);
        return clone;
    }
}
