using KrnlAI.VisualStudio.Extensibility.Core.Commands;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Editor;

namespace KrnlAI.VisualStudio.Extensibility.Commands;

/// <summary>
/// Copies the current editor selection as an analysis prompt to the clipboard
/// (equivalent of the legacy SendSelectionToChat command).
/// </summary>
[VisualStudioContribution]
public sealed class SendSelectionToChatCommand : Command
{
    private readonly SendSelectionHandler _handler;

    /// <summary>Creates a new instance.</summary>
    public SendSelectionToChatCommand(SendSelectionHandler handler)
    {
        _handler = handler;
    }

    /// <inheritdoc/>
    public override CommandConfiguration CommandConfiguration => new("%SendSelectionToChatDisplayName%")
    {
        TooltipText = "%SendSelectionToChatTooltip%",
        Placements = new[] { CommandPlacement.KnownPlacements.ToolsMenu }
    };

    /// <inheritdoc/>
    public override async Task ExecuteCommandAsync(
        IClientContext clientContext,
        CancellationToken cancellationToken)
    {
        var textView = await Extensibility.Editor().GetActiveTextViewAsync(clientContext, cancellationToken);
        if (textView is null)
            return;

        var selection = textView.Selection;
        if (selection.IsEmpty)
            return;

        var filePath = textView.FilePath ?? string.Empty;
        var language = System.IO.Path.GetExtension(filePath).TrimStart('.');

        await _handler.SendSelectionAsync(
            language: language,
            filePath: filePath,
            code: selection.Extent.ToString() ?? string.Empty,
            cancellationToken: cancellationToken);
    }
}