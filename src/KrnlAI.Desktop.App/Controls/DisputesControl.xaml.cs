using System.Windows.Controls;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Controls;

public sealed partial class DisputesControl : UserControl
{
    public DisputesControl() { InitializeComponent(); }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            if (DataContext is ViewModels.MainViewModel vm)
                vm.DisputesVM?.RefreshCommand.Execute(null);
        }
        catch (System.Exception ex) { KrnlLogger.Write($"DisputesControl.OnLoaded: {ex.Message}"); }
    }
}
