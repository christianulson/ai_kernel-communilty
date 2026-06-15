using System.Windows.Controls;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Controls;

public sealed partial class P2PPaymentsControl : UserControl
{
    public P2PPaymentsControl() { InitializeComponent(); }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            if (DataContext is ViewModels.MainViewModel vm)
                vm.P2PVM?.RefreshCommand.Execute(null);
        }
        catch (System.Exception ex) { KrnlLogger.Write($"P2PPaymentsControl.OnLoaded: {ex.Message}"); }
    }
}
