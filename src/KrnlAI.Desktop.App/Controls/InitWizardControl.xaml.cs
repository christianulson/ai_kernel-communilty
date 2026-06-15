using System.Windows.Controls;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Controls;
public sealed partial class InitWizardControl : UserControl
{
    public InitWizardControl() { InitializeComponent(); }
    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { try { } catch (System.Exception ex) { KrnlLogger.Write($"InitWizardControl: {ex.Message}"); } }
}
