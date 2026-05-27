namespace KrnlAI.VisualStudio.Services;

public static class KernelEndpointResolver
{
    private const string DefaultLocalApiEndpoint = "http://localhost:65335";

    public static string Resolve(KernelRuntimeMode mode, string? endpoint, int sidecarPort)
    {
        if (mode == KernelRuntimeMode.Embedded)
            return $"http://127.0.0.1:{NormalizePort(sidecarPort)}";

        var fallback = mode == KernelRuntimeMode.LocalApi
            ? DefaultLocalApiEndpoint
            : "https://api.krnlai.dev";

        if (string.IsNullOrWhiteSpace(endpoint))
            return fallback;

        var normalizedEndpoint = endpoint!.TrimEnd('/');
        if (!Uri.TryCreate(normalizedEndpoint, UriKind.Absolute, out var uri))
            return fallback;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return fallback;

        if (mode == KernelRuntimeMode.LocalApi && !IsLoopback(uri))
            return DefaultLocalApiEndpoint;

        return uri.ToString().TrimEnd('/');
    }

    private static bool IsLoopback(Uri uri)
    {
        return uri.IsLoopback
            || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || uri.Host == "127.0.0.1"
            || uri.Host == "::1";
    }

    private static int NormalizePort(int sidecarPort)
    {
        return sidecarPort is >= 1 and <= 65535 ? sidecarPort : 5001;
    }
}
