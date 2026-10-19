namespace KrnlAI.Desktop.Core.Abstractions;

/// <summary>
/// Resolves the Krnl-AI gateway API base URL from environment, settings and defaults.
/// Precedence: <c>KRNL__API_BASE_URL</c> env var &gt; configured value &gt; default local gateway.
/// </summary>
public static class ApiEndpointResolver
{
    /// <summary>Default local gateway URL.</summary>
    public const string DefaultGatewayUrl = "http://localhost:5235";

    private const string EnvironmentVariableName = "KRNL__API_BASE_URL";

    /// <summary>
    /// Resolves the base URL. Invalid values fall back to the default.
    /// </summary>
    public static string Resolve(string? configured)
    {
        var env = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(env) && IsValidHttpUrl(env))
            return env.TrimEnd('/');

        if (!string.IsNullOrWhiteSpace(configured) && IsValidHttpUrl(configured))
            return configured.TrimEnd('/');

        return DefaultGatewayUrl;
    }

    private static bool IsValidHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}