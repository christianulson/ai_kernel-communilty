using KrnlAI.VisualStudio.Extensibility.Core.Dashboard;
using KrnlAI.VisualStudio.Extensibility.Core.Services;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.RpcContracts.RemoteUI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>Krnl-AI dashboard tool window (Remote UI), probing the kernel.</summary>
[VisualStudioContribution]
public sealed class DashboardToolWindow : ToolWindow
{
    private readonly KernelClientService _kernel;
    private readonly DashboardToolWindowContent _content;

    /// <summary>Creates a new instance.</summary>
    public DashboardToolWindow(VisualStudioExtensibility extensibility)
        : base(extensibility)
    {
        Title = "Krnl-AI Dashboard";
        _kernel = new KernelClientService(new HttpClient(), maxRetries: 1);
        var model = new DashboardModel();
        var dataContext = new DashboardDataContext(model, _kernel);
        _content = new DashboardToolWindowContent(dataContext);
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
        {
            _content.Dispose();
            _kernel.Dispose();
        }

        base.Dispose(disposing);
    }
}