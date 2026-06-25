using System.Windows.Controls;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Controls;

public sealed partial class SnapshotsControl : UserControl
{
    public SnapshotsControl()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            if (DataContext is ViewModels.MainViewModel vm)
                await vm.SnapshotsVM.LoadAsync().ConfigureAwait(false);
        }
        catch (Exception ex) { KrnlLogger.Write($"SnapshotsControl.OnLoaded: {ex.Message}"); }
    }
}
