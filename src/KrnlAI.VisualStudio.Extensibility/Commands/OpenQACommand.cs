using KrnlAI.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace KrnlAI.VisualStudio.Extensibility.Commands;

/// <summary>Opens the Krnl-AI qa tool window.</summary>
[VisualStudioContribution]
public sealed class OpenQACommand : Command
{
    /// <inheritdoc/>
    public override CommandConfiguration CommandConfiguration => new("%OpenQACommandDisplayName%")
    {
        TooltipText = "%OpenQACommandTooltip%",
        Placements = new[] { CommandPlacement.KnownPlacements.ViewOtherWindowsMenu }
    };

    /// <inheritdoc/>
    public override Task ExecuteCommandAsync(IClientContext clientContext, CancellationToken cancellationToken)
        => Extensibility.Shell().ShowToolWindowAsync<QAToolWindow>(activate: true, cancellationToken);
}