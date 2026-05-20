using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

namespace KrnlAI.VisualStudio.ToolWindows.Policies;

[Guid("E2F3A4B5-C6D7-8E9F-0A1B-2C3D4E5F6A70")]
public sealed class PoliciesToolWindow : ToolWindowPane
{
    public PoliciesToolWindow() : base(null)
    {
        Caption = "Krnl-AI Policies";
        BitmapImageMoniker = KnownMonikers.StatusInformation;
        Content = new PoliciesControl();
    }
}
