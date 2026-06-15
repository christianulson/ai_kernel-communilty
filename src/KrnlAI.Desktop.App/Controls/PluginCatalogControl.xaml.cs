using System.Windows.Controls;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Controls;
public sealed partial class PluginCatalogControl : UserControl
{
    public PluginCatalogControl() { InitializeComponent(); }
    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { try { } catch (System.Exception ex) { KrnlLogger.Write($"PluginCatalogControl: {ex.Message}"); } }
}
