using System.Windows.Controls;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Controls;
public sealed partial class DebugControl : UserControl
{
    public DebugControl() { InitializeComponent(); }
    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { try { } catch (System.Exception ex) { KrnlLogger.Write($"DebugControl: {ex.Message}"); } }
}
