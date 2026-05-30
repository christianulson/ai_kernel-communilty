using System.Windows;
using System.Windows.Controls;

namespace KrnlAI.Desktop.App.Controls;

public sealed partial class SidecarConfigControl : UserControl
{
    public SidecarConfigControl()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
            vm.SidecarVM?.RefreshCommand.Execute(null);
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm && sender is PasswordBox pb)
            vm.SidecarVM.ApiKey = pb.Password;
    }
}
