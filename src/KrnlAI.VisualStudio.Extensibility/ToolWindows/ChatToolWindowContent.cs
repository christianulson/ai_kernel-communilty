using KrnlAI.VisualStudio.Extensibility.Core.Chat;
using Microsoft.VisualStudio.Extensibility.UI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>Krnl-AI chat tool window content (Remote UI).</summary>
public sealed class ChatToolWindowContent : RemoteUserControl
{
    /// <summary>Creates a new instance.</summary>
    public ChatToolWindowContent()
        : base(dataContext: new ChatDataContext(new ChatSession(new Core.Chat.LoggingMessageSink())))
    {
    }
}