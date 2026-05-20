using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

namespace KrnlAI.VisualStudio.ToolWindows.Episodes;

[Guid("F3A4B5C6-D7E8-9F0A-1B2C-3D4E5F6A7B80")]
public sealed class EpisodesToolWindow : ToolWindowPane
{
    public EpisodesToolWindow() : base(null)
    {
        Caption = "Krnl-AI Episodes";
        BitmapImageMoniker = KnownMonikers.StatusInformation;
        Content = new EpisodesControl();
    }
}
