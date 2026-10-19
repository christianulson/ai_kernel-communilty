using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

namespace KrnlAI.VisualStudio.ToolWindows.QA;

[Guid("C4D5E6F7-A8B9-0123-CDEF-234567890123")]
public sealed class QAToolWindow : ToolWindowPane
{
    public QAToolWindow() : base(null)
    {
        Caption = "Krnl-AI QA Tests";
        BitmapImageMoniker = KnownMonikers.StatusInformation;
        Content = new QAControl();
    }
}
