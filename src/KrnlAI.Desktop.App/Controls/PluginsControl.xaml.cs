using System.Windows.Controls;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Controls;

public sealed partial class PluginsControl : UserControl
{
    public PluginsControl() { InitializeComponent(); }
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { try { if (DataContext is ViewModels.MainViewModel vm) await vm.PluginsVM.LoadAsync(); } catch (Exception ex) { KrnlLogger.Write($"PluginsControl: {ex.Message}"); } }
}
