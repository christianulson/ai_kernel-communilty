using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.RpcContracts.RemoteUI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>Krnl-AI qa tool window (Remote UI).</summary>
[VisualStudioContribution]
public sealed class QAToolWindow : ToolWindow
{
    private readonly QAToolWindowContent _content;

    /// <summary>Creates a new instance.</summary>
    public QAToolWindow(VisualStudioExtensibility extensibility)
        : base(extensibility)
    {
        Title = "Krnl-AI QA";
        _content = new QAToolWindowContent();
    }

    /// <inheritdoc/>
    public override ToolWindowConfiguration ToolWindowConfiguration => new()
    {
        Placement = ToolWindowPlacement.DocumentWell,
        AllowAutoCreation = true
    };

    /// <inheritdoc/>
    public override Task<IRemoteUserControl> GetContentAsync(CancellationToken cancellationToken)
        => Task.FromResult<IRemoteUserControl>(_content);

    /// <inheritdoc/>
    public override Task InitializeAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _content.Dispose();

        base.Dispose(disposing);
    }
}