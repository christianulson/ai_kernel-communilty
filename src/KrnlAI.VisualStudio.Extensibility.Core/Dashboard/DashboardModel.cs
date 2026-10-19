namespace KrnlAI.VisualStudio.Extensibility.Core.Dashboard;

/// <summary>
/// Pure dashboard state (UI-agnostic).
/// </summary>
public sealed class DashboardModel
{
    /// <summary>Kernel connection status.</summary>
    public enum ConnectionStatus
    {
        /// <summary>Not yet checked.</summary>
        Unknown,

        /// <summary>Kernel is reachable.</summary>
        Connected,

        /// <summary>Kernel is not reachable.</summary>
        Disconnected
    }

    /// <summary>Extension version.</summary>
    public string Version => "1.0.0";

    /// <summary>Current kernel connection status.</summary>
    public ConnectionStatus KernelStatus { get; private set; } = ConnectionStatus.Unknown;

    /// <summary>Updates the kernel connection status.</summary>
    public void UpdateKernelStatus(ConnectionStatus status)
    {
        KernelStatus = status;
    }
}