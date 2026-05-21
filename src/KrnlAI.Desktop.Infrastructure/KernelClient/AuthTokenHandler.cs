using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace KrnlAI.Desktop.Infrastructure.KernelClient;

public sealed class AuthTokenHandler : DelegatingHandler
{
    private readonly AuthTokenProvider _tokenProvider;
    private readonly Func<CancellationToken, Task<string?>>? _refreshHandler;

    public AuthTokenHandler(AuthTokenProvider tokenProvider, Func<CancellationToken, Task<string?>>? refreshHandler = null)
    {
        _tokenProvider = tokenProvider;
        _refreshHandler = refreshHandler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_tokenProvider.Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenProvider.Token);

        var response = await base.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized
            && _tokenProvider.RefreshToken != null
            && _refreshHandler != null
            && !IsRefreshRequest(request))
        {
            var newToken = await _refreshHandler(ct);
            if (newToken != null)
            {
                _tokenProvider.Token = newToken;
                var retry = await CloneRequestAsync(request, ct);
                retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                response.Dispose();
                return await base.SendAsync(retry, ct);
            }
            _tokenProvider.Clear();
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
            var body = await request.Content.ReadAsByteArrayAsync(ct);
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
