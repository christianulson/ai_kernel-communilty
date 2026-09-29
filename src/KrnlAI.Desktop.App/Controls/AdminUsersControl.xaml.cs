using System.Windows;
using System.Windows.Controls;
using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Controls;

public sealed partial class AdminUsersControl : UserControl
{
    public AdminUsersControl()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is MainViewModel vm)
                await vm.AdminUsersVM.LoadAsync();
        }
        catch (Exception ex) { KrnlLogger.Write($"AdminUsersControl.OnLoaded: {ex.Message}"); }
    }

    private void OnActivateClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || vm.AdminUsersVM.SelectedUser == null) return;
        var user = vm.AdminUsersVM.SelectedUser;
        if (!ConfirmModal.Show(Window.GetWindow(this), "Ativar usuário",
                $"Ativar usuário {user.Name} ({user.Email})?", danger: false)) return;
        vm.AdminUsersVM.ActivateCommand.Execute(null);
    }

    private void OnSuspendClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || vm.AdminUsersVM.SelectedUser == null) return;
        var user = vm.AdminUsersVM.SelectedUser;
        if (!ConfirmModal.Show(Window.GetWindow(this), "Suspender usuário",
                $"Suspender usuário {user.Name} ({user.Email})?", danger: true)) return;
        vm.AdminUsersVM.SuspendCommand.Execute(null);
    }
}
