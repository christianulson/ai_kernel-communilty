using KrnlAI.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace KrnlAI.VisualStudio.Extensibility.Commands;

/// <summary>Opens the Krnl-AI chat tool window.</summary>
[VisualStudioContribution]
public sealed class OpenKrnlAIChatCommand : Command
{
    /// <inheritdoc/>
    public override CommandConfiguration CommandConfiguration => new("%OpenChatCommandDisplayName%")
    {
        TooltipText = "%OpenChatCommandTooltip%",
        Placements = new[] { CommandPlacement.KnownPlacements.ViewOtherWindowsMenu }
    };

    /// <inheritdoc/>
    public override Task ExecuteCommandAsync(IClientContext clientContext, CancellationToken cancellationToken)
        => Extensibility.Shell().ShowToolWindowAsync<ChatToolWindow>(activate: true, cancellationToken);
}