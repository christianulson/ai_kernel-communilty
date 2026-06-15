using System.Windows.Controls;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Controls;

public sealed partial class MomentsControl : UserControl
{
    public MomentsControl() { InitializeComponent(); }
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { try { if (DataContext is ViewModels.MainViewModel vm) await vm.MomentsVM.LoadAsync(); } catch (Exception ex) { KrnlLogger.Write($"MomentsControl: {ex.Message}"); } }
}
