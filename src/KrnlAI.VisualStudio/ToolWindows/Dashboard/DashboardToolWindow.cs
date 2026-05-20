using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

namespace KrnlAI.VisualStudio.ToolWindows.Dashboard;

[Guid("D1E2F3A4-B5C6-7D8E-9F0A-1B2C3D4E5F60")]
public sealed class DashboardToolWindow : ToolWindowPane
{
    public DashboardToolWindow() : base(null)
    {
        Caption = "Krnl-AI Dashboard";
        BitmapImageMoniker = KnownMonikers.StatusInformation;
        Content = new DashboardControl();
    }
}
