using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

namespace KrnlAI.VisualStudio.ToolWindows;

[Guid("E5F4A3B2-C1D0-4E5F-8A9B-0C1D2E3F4A5B")]
public sealed class DebugToolWindow : ToolWindowPane
{
    public DebugToolWindow() : base(null)
    {
        Caption = "Krnl-AI Debug";
        BitmapImageMoniker = KnownMonikers.StatusInformation;
        Content = new DebugToolWindowControl();
    }
}
