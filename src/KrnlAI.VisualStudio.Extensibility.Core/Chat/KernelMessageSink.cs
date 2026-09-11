using KrnlAI.Sdk.Models;
using KrnlAI.VisualStudio.Extensibility.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KrnlAI.VisualStudio.Extensibility.Core.Chat;

/// <summary>
/// Message sink that forwards chat messages to the Krnl-AI kernel and raises
/// the assistant response for the chat UI.
/// </summary>
public sealed class KernelMessageSink : IMessageSink
{
    private readonly IKernelClientService _kernel;
    private readonly ILogger<KernelMessageSink> _logger;

    /// <summary>Creates a new instance.</summary>
    public KernelMessageSink(IKernelClientService kernel, ILogger<KernelMessageSink>? logger = null)
    {
        _kernel = kernel;
        _logger = logger ?? NullLogger<KernelMessageSink>.Instance;
    }

    /// <summary>Raised when the kernel returns an assistant message.</summary>
    public event Action<ChatMessage>? AssistantMessageReceived;

    /// <inheritdoc/>
    public async Task SendAsync(string message, CancellationToken cancellationToken)
    {
        if (_kernel.State != ConnectionState.Connected)
        {
            _logger.LogInformation("Kernel not connected; chat message logged only");
            return;
        }

        try
        {
            var response = await _kernel.RunAgentAsync(message, ct: cancellationToken);
            var text = !string.IsNullOrWhiteSpace(response.Summary)
                ? response.Summary
                : response.Status ?? "completed";
            AssistantMessageReceived?.Invoke(new ChatMessage("assistant", text));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Agent run failed for chat message");
        }
    }
}