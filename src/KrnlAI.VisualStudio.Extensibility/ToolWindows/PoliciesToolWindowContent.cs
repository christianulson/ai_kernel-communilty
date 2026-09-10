using KrnlAI.VisualStudio.Extensibility.Core.Policies;
using Microsoft.VisualStudio.Extensibility.UI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>Policies tool window content (Remote UI).</summary>
public sealed class PoliciesToolWindowContent : RemoteUserControl
{
    /// <summary>Creates a new instance.</summary>
    public PoliciesToolWindowContent()
        : base(dataContext: new PoliciesDataContext(new PoliciesModel()))
    {
    }
}