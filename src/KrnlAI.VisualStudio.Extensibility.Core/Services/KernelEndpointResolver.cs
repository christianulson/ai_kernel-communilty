namespace KrnlAI.VisualStudio.Extensibility.Core.Services;

/// <summary>Runtime mode of the Krnl-AI kernel connection.</summary>
public enum KernelRuntimeMode
{
    /// <summary>Local kernel API (default localhost gateway).</summary>
    LocalApi = 0,

    /// <summary>Embedded sidecar runtime.</summary>
    Embedded = 1,

    /// <summary>Cloud Krnl-AI API.</summary>
    Cloud = 2
}

/// <summary>
/// Resolves the kernel endpoint based on runtime mode and configuration.
/// </summary>
public static class KernelEndpointResolver
{
    private const string DefaultLocalApiEndpoint = "http://localhost:5235";

    /// <summary>Resolves the endpoint for the given mode and input.</summary>
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