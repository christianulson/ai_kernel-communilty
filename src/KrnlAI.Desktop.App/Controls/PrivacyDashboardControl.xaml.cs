using System.Windows;
using System.Windows.Controls;

namespace KrnlAI.Desktop.App.Controls;

public partial class PrivacyDashboardControl : UserControl
{
    public PrivacyDashboardControl()
    {
        InitializeComponent();
    }

    private void OnRequestDeletion(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Data deletion request submitted.", "Privacy", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnRequestExport(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Data export request submitted.", "Privacy", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
