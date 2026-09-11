using KrnlAI.VisualStudio.Extensibility.Core.Services;

namespace KrnlAI.VisualStudio.Extensibility.Core.Dashboard;

/// <summary>
/// Probes the kernel health and updates the dashboard model status.
/// </summary>
public static class DashboardHealthProbe
{
    /// <summary>Probes the kernel and updates <paramref name="model"/>.</summary>
    public static async Task ProbeAsync(
        IKernelClientService kernel,
        DashboardModel model,
        CancellationToken cancellationToken)
    {
        try
        {
            var healthy = await kernel.CheckHealthAsync(cancellationToken);
            model.UpdateKernelStatus(
                healthy ? DashboardModel.ConnectionStatus.Connected : DashboardModel.ConnectionStatus.Disconnected);
        }
        catch
        {
            model.UpdateKernelStatus(DashboardModel.ConnectionStatus.Disconnected);
        }
    }
}