using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

namespace KrnlAI.VisualStudio.ToolWindows.Backlog;

[Guid("B3C4D5E6-F7A8-9012-BCDE-F12345678902")]
public sealed class BacklogToolWindow : ToolWindowPane
{
    public BacklogToolWindow() : base(null)
    {
        Caption = "Krnl-AI Backlog";
        BitmapImageMoniker = KnownMonikers.StatusInformation;
        Content = new BacklogControl();
    }
}
