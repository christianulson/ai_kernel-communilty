namespace KrnlAI.Desktop.Infrastructure.KernelClient;

public sealed class DynamicBaseUrlHandler : DelegatingHandler
{
    private const string DefaultBaseUrl = "http://localhost:5235";
    private static string _baseUrl = DefaultBaseUrl;

    public static void SetBaseUrl(string url)
    {
        _baseUrl = url.TrimEnd('/');
    }

    public static void ResetBaseUrl()
    {
        _baseUrl = DefaultBaseUrl;
    }

    public DynamicBaseUrlHandler()
    {
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.PathAndQuery;
        request.RequestUri = new Uri(_baseUrl + path);
        return base.SendAsync(request, cancellationToken);
    }
}

