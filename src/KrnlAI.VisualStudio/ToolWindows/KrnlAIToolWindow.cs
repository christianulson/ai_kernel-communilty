using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

namespace KrnlAI.VisualStudio.ToolWindows;

[Guid("B2C3D4E5-F6A7-8901-BCDE-F12345678901")]
public sealed class KrnlAIToolWindow : ToolWindowPane
{
    public KrnlAIToolWindow() : base(null)
    {
        Caption = "Krnl-AI";
        BitmapImageMoniker = KnownMonikers.StatusInformation;
        Content = new KrnlAIToolWindowControl();
    }
}
