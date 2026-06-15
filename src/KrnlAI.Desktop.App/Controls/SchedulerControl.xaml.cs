using System.Windows.Controls;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Controls;

public sealed partial class SchedulerControl : UserControl
{
    public SchedulerControl() { InitializeComponent(); }
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { try { if (DataContext is ViewModels.MainViewModel vm) await vm.SchedulerVM.LoadAsync(); } catch (Exception ex) { KrnlLogger.Write($"SchedulerControl: {ex.Message}"); } }
}
