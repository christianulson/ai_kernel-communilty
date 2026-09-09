using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KrnlAI.VisualStudio.Extensibility.Core.CodeLens;

/// <summary>
/// Fallback implementation that records the explain request until chat integration lands.
/// </summary>
public sealed class LoggingExplainService : IExplainService
{
    private readonly ILogger<LoggingExplainService> _logger;

    /// <summary>Creates a new instance.</summary>
    public LoggingExplainService(ILogger<LoggingExplainService>? logger = null)
    {
        _logger = logger ?? NullLogger<LoggingExplainService>.Instance;
    }

    /// <inheritdoc/>
    public Task ExplainAsync(string elementIdentifier, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Explain request for '{ElementIdentifier}'", elementIdentifier);
        return Task.CompletedTask;
    }
}