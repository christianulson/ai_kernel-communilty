using System.Windows;
using System.Windows.Controls;
using KrnlAI.Desktop.Core.Services;
using KrnlAI.Desktop.Infrastructure.Abstractions;

namespace KrnlAI.Desktop.App.Controls;

public partial class AdminUsersControl : UserControl
{
    private List<UserInfo> _users = new();

    public AdminUsersControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UsersGrid.ItemsSource = _users;
    }

    private void OnUserSelected(object sender, SelectionChangedEventArgs e)
    {
        var selected = UsersGrid.SelectedItem is UserInfo;
        ActivateButton.IsEnabled = selected;
        SuspendButton.IsEnabled = selected;
    }

    private void OnActivate(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not UserInfo user) return;
        try
        {
            KrnlLogger.Write($"Activate user: {user.Id}");
            MessageBox.Show($"User {user.Name} activated.", "Admin", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
        }
    }

    private void OnSuspend(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not UserInfo user) return;
        try
        {
            KrnlLogger.Write($"Suspend user: {user.Id}");
            MessageBox.Show($"User {user.Name} suspended.", "Admin", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
        }
    }
}
