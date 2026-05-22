using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

namespace KrnlAI.VisualStudio.ToolWindows.Kanban;

[Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890")]
public sealed class KanbanToolWindow : ToolWindowPane
{
    public KanbanToolWindow() : base(null)
    {
        Caption = "Krnl-AI Kanban";
        BitmapImageMoniker = KnownMonikers.StatusInformation;
        Content = new KanbanControl();
    }
}
