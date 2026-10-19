using KrnlAI.VisualStudio.Extensibility.Core.Debug;
using Microsoft.VisualStudio.Extensibility.UI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>Debug tool window content (Remote UI).</summary>
public sealed class DebugToolWindowContent : RemoteUserControl
{
    /// <summary>Creates a new instance.</summary>
    public DebugToolWindowContent()
        : base(dataContext: new DebugDataContext(new DebugModel()))
    {
    }
}