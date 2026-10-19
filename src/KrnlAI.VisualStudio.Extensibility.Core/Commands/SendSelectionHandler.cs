using KrnlAI.VisualStudio.Extensibility.Core.Prompts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KrnlAI.VisualStudio.Extensibility.Core.Commands;

/// <summary>Abstraction over the OS clipboard.</summary>
public interface IClipboard
{
    /// <summary>Sets the clipboard text.</summary>
    Task SetTextAsync(string text, CancellationToken cancellationToken);
}

/// <summary>
/// Coordinates the "Send selection to chat" flow: build prompt and copy it.
/// </summary>
public sealed class SendSelectionHandler
{
    private readonly IClipboard _clipboard;
    private readonly ILogger<SendSelectionHandler> _logger;

    /// <summary>Creates a new instance.</summary>
    public SendSelectionHandler(IClipboard clipboard, ILogger<SendSelectionHandler>? logger = null)
    {
        _clipboard = clipboard;
        _logger = logger ?? NullLogger<SendSelectionHandler>.Instance;
    }

    /// <summary>Builds the prompt for the given selection and copies it to the clipboard.</summary>
    public async Task SendSelectionAsync(
        string language,
        string filePath,
        string code,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(code))
            return;

        var prompt = AnalyzePromptBuilder.BuildCodePrompt(language, filePath, code);
        await _clipboard.SetTextAsync(prompt, cancellationToken);
        _logger.LogInformation("Code selection prompt copied to clipboard ({CodeLength} chars)", code.Length);
    }
}