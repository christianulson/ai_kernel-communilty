using KrnlAI.VisualStudio.Extensibility.Core.Dashboard;
using Microsoft.VisualStudio.Extensibility.UI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>Krnl-AI dashboard tool window content (Remote UI).</summary>
public sealed class DashboardToolWindowContent : RemoteUserControl
{
    private readonly DashboardDataContext _dataContext;
    private System.Threading.Timer? _pollTimer;

    /// <summary>Creates a new instance with the given data context.</summary>
    public DashboardToolWindowContent(DashboardDataContext dataContext)
        : base(dataContext: dataContext)
    {
        _dataContext = dataContext;
    }

    /// <inheritdoc/>
    public override async Task ControlLoadedAsync(CancellationToken cancellationToken)
    {
        await base.ControlLoadedAsync(cancellationToken);
        await _dataContext.RefreshCommand.ExecuteAsync(null, null!, cancellationToken);

        _pollTimer = new System.Threading.Timer(
            _ => _ = RefreshPeriodicallyAsync(),
            null,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30));
    }

    private async Task RefreshPeriodicallyAsync()
    {
        try
        {
            await _dataContext.RefreshCommand.ExecuteAsync(null, null!, CancellationToken.None);
        }
        catch
        {
            // polling is best-effort; the next tick will retry
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _pollTimer?.Dispose();

        base.Dispose(disposing);
    }
}