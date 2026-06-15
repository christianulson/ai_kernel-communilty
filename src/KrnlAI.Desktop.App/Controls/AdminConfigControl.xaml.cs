using System.Windows;
using System.Windows.Controls;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Controls;

public sealed partial class AdminConfigControl : UserControl
{
    public AdminConfigControl()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is ViewModels.MainViewModel vm)
                await vm.AdminConfigVM.LoadAsync();
        }
        catch (Exception ex) { KrnlLogger.Write($"AdminConfigControl.OnLoaded: {ex.Message}"); }
    }

    private async void OnFlagToggled(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                vm.AdminConfigVM.StatusMessage = "Toggle via API não implementado. Use o servidor admin.";
                await vm.AdminConfigVM.LoadAsync();
            }
        }
        catch (Exception ex) { KrnlLogger.Write($"AdminConfigControl.OnFlagToggled: {ex.Message}"); }
    }
}
