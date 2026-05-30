using System.Windows.Controls;

namespace KrnlAI.Desktop.App.Controls;

public sealed partial class DisputesControl : UserControl
{
    public DisputesControl()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
            vm.DisputesVM?.RefreshCommand.Execute(null);
    }
}
