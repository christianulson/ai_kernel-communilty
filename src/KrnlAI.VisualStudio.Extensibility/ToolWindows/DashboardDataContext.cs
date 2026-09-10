using System.Runtime.Serialization;
using KrnlAI.VisualStudio.Extensibility.Core.Dashboard;
using Microsoft.VisualStudio.Extensibility.UI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>
/// Data context for the Krnl-AI dashboard tool window (Remote UI).
/// </summary>
[DataContract]
public sealed class DashboardDataContext : NotifyPropertyChangedObject
{
    private readonly DashboardModel _model;

    /// <summary>Creates a new instance wrapping the given model.</summary>
    public DashboardDataContext(DashboardModel model)
    {
        _model = model;
    }

    /// <summary>Extension version.</summary>
    [DataMember]
    public string Version => _model.Version;

    /// <summary>Kernel connection status text.</summary>
    [DataMember]
    public string KernelStatus => _model.KernelStatus.ToString();
}