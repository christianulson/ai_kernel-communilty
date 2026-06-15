using System.Windows.Controls;

namespace KrnlAI.Desktop.App.Controls;

public sealed partial class ObjectivesControl : UserControl
{
    public ObjectivesControl()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            if (DataContext is ViewModels.MainViewModel vm)
                await vm.ObjectivesVM.LoadAsync();
        }
        catch { }
    }
}
