using KrnlAI.VisualStudio.Extensibility.Core.Backlog;
using Microsoft.VisualStudio.Extensibility.UI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>Backlog tool window content (Remote UI).</summary>
public sealed class BacklogToolWindowContent : RemoteUserControl
{
    /// <summary>Creates a new instance.</summary>
    public BacklogToolWindowContent()
        : base(dataContext: new BacklogDataContext(new BacklogModel()))
    {
    }
}