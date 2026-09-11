using Microsoft.VisualStudio.Extensibility.UI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>Krnl-AI chat tool window content (Remote UI).</summary>
public sealed class ChatToolWindowContent : RemoteUserControl
{
    /// <summary>Creates a new instance with the given data context.</summary>
    public ChatToolWindowContent(ChatDataContext dataContext)
        : base(dataContext: dataContext)
    {
    }
}