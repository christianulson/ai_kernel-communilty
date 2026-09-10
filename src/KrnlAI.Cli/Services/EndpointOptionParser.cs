namespace KrnlAI.Cli.Services;

/// <summary>
/// Resolves the kernel API base URL from CLI arguments without throwing
/// when <c>--endpoint</c> is the last argument.
/// </summary>
public static class EndpointOptionParser
{
    /// <summary>Default kernel API endpoint.</summary>
    public const string DefaultEndpoint = "http://localhost:5235";

    /// <summary>
    /// Resolves the endpoint from args, falling back to the default on
    /// missing flag, missing value or malformed input. Never throws.
    /// </summary>
    public static bool TryResolve(string[] args, out string endpoint)
    {
        endpoint = DefaultEndpoint;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "--endpoint")
            {
                if (i + 1 < args.Length)
                    endpoint = args[i + 1];
                return true;
            }

            if (arg.StartsWith("--endpoint=", StringComparison.OrdinalIgnoreCase))
            {
                var value = arg["--endpoint=".Length..];
                if (!string.IsNullOrWhiteSpace(value))
                    endpoint = value;
                return true;
            }
        }

        return true;
    }
}