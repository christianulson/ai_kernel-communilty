namespace KrnlAI.Sidecar;

/// <summary>
/// Proxies requests to the Kernel API when configured. Falls back gracefully when unreachable.
/// Registered as singleton — safe for concurrent use.
/// </summary>
public sealed class KernelApiProxy(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<KernelApiProxy> logger)
{
    private readonly string _baseUrl = configuration.GetValue<string>("Sidecar:KernelApi:BaseUrl") ?? "";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_baseUrl);

    public async Task<T?> ProxyGetAsync<T>(string path, CancellationToken ct) where T : class
    {
        if (!IsConfigured) return null;
        try
        {
            var client = httpClientFactory.CreateClient("kernel");
            var res = await client.GetAsync($"{_baseUrl}{path}", ct);
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Kernel API proxy GET {Path} failed, falling back to local", path);
            return null;
        }
    }

    public async Task<TResponse?> ProxyPostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct)
        where TResponse : class
    {
        if (!IsConfigured) return null;
        try
        {
            var client = httpClientFactory.CreateClient("kernel");
            var res = await client.PostAsJsonAsync($"{_baseUrl}{path}", body, ct);
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Kernel API proxy POST {Path} failed, falling back to local", path);
            return null;
        }
    }
}
