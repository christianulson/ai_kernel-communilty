using KrnlAI.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace KrnlAI.VisualStudio.Extensibility.Commands;

/// <summary>Opens the Krnl-AI policies tool window.</summary>
[VisualStudioContribution]
public sealed class OpenPoliciesCommand : Command
{
    /// <inheritdoc/>
    public override CommandConfiguration CommandConfiguration => new("%OpenPoliciesCommandDisplayName%")
    {
        TooltipText = "%OpenPoliciesCommandTooltip%",
        Placements = new[] { CommandPlacement.KnownPlacements.ViewOtherWindowsMenu }
    };

    /// <inheritdoc/>
    public override Task ExecuteCommandAsync(IClientContext clientContext, CancellationToken cancellationToken)
        => Extensibility.Shell().ShowToolWindowAsync<PoliciesToolWindow>(activate: true, cancellationToken);
}