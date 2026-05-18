namespace KrnlAI.Desktop.Infrastructure.KernelClient;

public sealed class DynamicBaseUrlHandler : DelegatingHandler
{
    private static string _baseUrl = "http://localhost:5000";

    public static void SetBaseUrl(string url)
    {
        _baseUrl = url.TrimEnd('/');
    }

    public DynamicBaseUrlHandler()
        : base(new HttpClientHandler())
    {
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.PathAndQuery;
        request.RequestUri = new Uri(_baseUrl + path);
        return base.SendAsync(request, cancellationToken);
    }
}

