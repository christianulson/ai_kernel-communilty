using System.Windows.Controls;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Controls;

public sealed partial class InvestigationsControl : UserControl
{
    public InvestigationsControl()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            if (DataContext is ViewModels.MainViewModel vm)
                await vm.InvestigationsVM.LoadAsync().ConfigureAwait(false);
        }
        catch (Exception ex) { KrnlLogger.Write($"InvestigationsControl.OnLoaded: {ex.Message}"); }
    }
}
