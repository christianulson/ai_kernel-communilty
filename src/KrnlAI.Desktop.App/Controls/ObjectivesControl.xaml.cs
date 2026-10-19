using System.Windows.Controls;
using KrnlAI.Desktop.Core.Services;

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
        catch (Exception ex) { KrnlLogger.Write($"ObjectivesControl.OnLoaded: {ex.Message}"); }
    }
}
