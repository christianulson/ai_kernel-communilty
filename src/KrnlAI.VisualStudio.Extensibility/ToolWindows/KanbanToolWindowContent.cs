using KrnlAI.VisualStudio.Extensibility.Core.Kanban;
using Microsoft.VisualStudio.Extensibility.UI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>Kanban tool window content (Remote UI).</summary>
public sealed class KanbanToolWindowContent : RemoteUserControl
{
    /// <summary>Creates a new instance.</summary>
    public KanbanToolWindowContent()
        : base(dataContext: new KanbanDataContext(new KanbanModel()))
    {
    }
}