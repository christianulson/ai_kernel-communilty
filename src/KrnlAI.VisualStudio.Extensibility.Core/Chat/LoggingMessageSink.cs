using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KrnlAI.VisualStudio.Extensibility.Core.Chat;

/// <summary>
/// Fallback message sink that records the message until the kernel client lands.
/// </summary>
public sealed class LoggingMessageSink : IMessageSink
{
    private readonly ILogger<LoggingMessageSink> _logger;

    /// <summary>Creates a new instance.</summary>
    public LoggingMessageSink(ILogger<LoggingMessageSink>? logger = null)
    {
        _logger = logger ?? NullLogger<LoggingMessageSink>.Instance;
    }

    /// <inheritdoc/>
    public Task SendAsync(string message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Chat message sent: {MessageLength} chars", message.Length);
        return Task.CompletedTask;
    }
}