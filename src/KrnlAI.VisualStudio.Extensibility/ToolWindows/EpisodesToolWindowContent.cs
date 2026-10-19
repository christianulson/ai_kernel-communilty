using KrnlAI.VisualStudio.Extensibility.Core.Episodes;
using Microsoft.VisualStudio.Extensibility.UI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>Episodes tool window content (Remote UI).</summary>
public sealed class EpisodesToolWindowContent : RemoteUserControl
{
    /// <summary>Creates a new instance.</summary>
    public EpisodesToolWindowContent()
        : base(dataContext: new EpisodesDataContext(new EpisodesModel()))
    {
    }
}