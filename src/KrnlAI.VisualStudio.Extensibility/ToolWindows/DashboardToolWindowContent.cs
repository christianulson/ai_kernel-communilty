using KrnlAI.VisualStudio.Extensibility.Core.Dashboard;
using Microsoft.VisualStudio.Extensibility.UI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>Krnl-AI dashboard tool window content (Remote UI).</summary>
public sealed class DashboardToolWindowContent : RemoteUserControl
{
    /// <summary>Creates a new instance.</summary>
    public DashboardToolWindowContent()
        : base(dataContext: new DashboardDataContext(new DashboardModel()))
    {
    }
}