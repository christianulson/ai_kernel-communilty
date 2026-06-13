namespace KrnlAI.Desktop.Infrastructure.KernelClient;

public sealed class DynamicBaseUrlHandler : DelegatingHandler
{
    private const string DefaultBaseUrl = "http://localhost:5235";
    private static readonly object _lock = new();
    private static string _baseUrl = DefaultBaseUrl;

    public static void SetBaseUrl(string url)
    {
        lock (_lock)
        {
            _baseUrl = url.TrimEnd('/');
        }
    }

    public static void ResetBaseUrl()
    {
        lock (_lock)
        {
            _baseUrl = DefaultBaseUrl;
        }
    }

    public static string GetBaseUrl()
    {
        lock (_lock)
        {
            return _baseUrl;
        }
    }

    public DynamicBaseUrlHandler()
    {
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var baseUrl = GetBaseUrl();
        var path = request.RequestUri!.PathAndQuery;
        request.RequestUri = new Uri(baseUrl + path);
        return base.SendAsync(request, cancellationToken);
    }
}

