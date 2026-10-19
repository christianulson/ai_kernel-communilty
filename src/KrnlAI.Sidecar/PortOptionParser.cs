namespace KrnlAI.Sidecar;

/// <summary>
/// Resolves the listening port from CLI arguments and configuration without
/// throwing when <c>--port</c> is the last argument or the value is invalid.
/// </summary>
public static class PortOptionParser
{
    /// <summary>Default listening port.</summary>
    public const int DefaultPort = 5001;

    /// <summary>
    /// Resolves the port. Precedence: CLI <c>--port</c> &gt; <paramref name="configValue"/> &gt; <see cref="DefaultPort"/>.
    /// Invalid values fall back to the default.
    /// </summary>
    public static int Resolve(string[] args, string? configValue = null)
    {
        string? fromArgs = null;

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--port" && i + 1 < args.Length)
            {
                fromArgs = args[i + 1];
                break;
            }

            if (args[i].StartsWith("--port=", StringComparison.OrdinalIgnoreCase))
            {
                fromArgs = args[i]["--port=".Length..];
                break;
            }
        }

        var raw = fromArgs ?? configValue;
        return ParsePort(raw);
    }

    private static int ParsePort(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || !int.TryParse(raw, out var port))
            return DefaultPort;

        return port is >= 1 and <= 65535 ? port : DefaultPort;
    }
}