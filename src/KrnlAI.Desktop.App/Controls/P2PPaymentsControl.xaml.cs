using System.Windows.Controls;

namespace KrnlAI.Desktop.App.Controls;

public sealed partial class P2PPaymentsControl : UserControl
{
    public P2PPaymentsControl()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
            vm.P2PVM?.RefreshCommand.Execute(null);
    }
}
