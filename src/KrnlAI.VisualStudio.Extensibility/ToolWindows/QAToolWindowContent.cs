using KrnlAI.VisualStudio.Extensibility.Core.QA;
using Microsoft.VisualStudio.Extensibility.UI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>QA tool window content (Remote UI).</summary>
public sealed class QAToolWindowContent : RemoteUserControl
{
    /// <summary>Creates a new instance.</summary>
    public QAToolWindowContent()
        : base(dataContext: new QADataContext(new QAModel()))
    {
    }
}