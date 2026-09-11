using System.Runtime.Serialization;
using KrnlAI.VisualStudio.Extensibility.Core.Dashboard;
using KrnlAI.VisualStudio.Extensibility.Core.Services;
using Microsoft.VisualStudio.Extensibility.UI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>
/// Data context for the Krnl-AI dashboard tool window (Remote UI).
/// </summary>
[DataContract]
public sealed class DashboardDataContext : NotifyPropertyChangedObject
{
    private readonly DashboardModel _model;
    private readonly IKernelClientService _kernel;
    private string _kernelStatus = DashboardModel.ConnectionStatus.Unknown.ToString();

    /// <summary>Creates a new instance wrapping the given model.</summary>
    public DashboardDataContext(DashboardModel model, IKernelClientService kernel)
    {
        _model = model;
        _kernel = kernel;
        RefreshCommand = new AsyncCommand(async (parameter, cancellationToken) =>
        {
            await DashboardHealthProbe.ProbeAsync(kernel, model, cancellationToken);
            KernelStatus = model.KernelStatus.ToString();
        });
    }

    /// <summary>Extension version.</summary>
    [DataMember]
    public string Version => _model.Version;

    /// <summary>Kernel connection status text.</summary>
    [DataMember]
    public string KernelStatus
    {
        get => _kernelStatus;
        private set => SetProperty(ref _kernelStatus, value);
    }

    /// <summary>Refresh command bound to the UI button.</summary>
    [DataMember]
    public AsyncCommand RefreshCommand { get; }
}