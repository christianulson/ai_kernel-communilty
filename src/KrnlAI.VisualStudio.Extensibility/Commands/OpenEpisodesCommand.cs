using KrnlAI.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace KrnlAI.VisualStudio.Extensibility.Commands;

/// <summary>Opens the Krnl-AI episodes tool window.</summary>
[VisualStudioContribution]
public sealed class OpenEpisodesCommand : Command
{
    /// <inheritdoc/>
    public override CommandConfiguration CommandConfiguration => new("%OpenEpisodesCommandDisplayName%")
    {
        TooltipText = "%OpenEpisodesCommandTooltip%",
        Placements = new[] { CommandPlacement.KnownPlacements.ViewOtherWindowsMenu }
    };

    /// <inheritdoc/>
    public override Task ExecuteCommandAsync(IClientContext clientContext, CancellationToken cancellationToken)
        => Extensibility.Shell().ShowToolWindowAsync<EpisodesToolWindow>(activate: true, cancellationToken);
}