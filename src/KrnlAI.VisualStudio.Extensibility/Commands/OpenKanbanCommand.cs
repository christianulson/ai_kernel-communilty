using KrnlAI.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace KrnlAI.VisualStudio.Extensibility.Commands;

/// <summary>Opens the Krnl-AI kanban tool window.</summary>
[VisualStudioContribution]
public sealed class OpenKanbanCommand : Command
{
    /// <inheritdoc/>
    public override CommandConfiguration CommandConfiguration => new("%OpenKanbanCommandDisplayName%")
    {
        TooltipText = "%OpenKanbanCommandTooltip%",
        Placements = new[] { CommandPlacement.KnownPlacements.ViewOtherWindowsMenu }
    };

    /// <inheritdoc/>
    public override Task ExecuteCommandAsync(IClientContext clientContext, CancellationToken cancellationToken)
        => Extensibility.Shell().ShowToolWindowAsync<KanbanToolWindow>(activate: true, cancellationToken);
}