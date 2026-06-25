using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Services;
using KrnlAI.Desktop.Core.Models;
namespace KrnlAI.Desktop.App.Controls;
public partial class SettingsControl : UserControl
{
    public SettingsControl() { InitializeComponent(); }

    private async void OnMcpToggled(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is CheckBox { DataContext: McpServerInfo server } check)
            {
                if (DataContext is MainViewModel mainVm)
                    await mainVm.SettingsVM.ToggleMcpServerAsync(server.ServerId, check.IsChecked ?? false).ConfigureAwait(false);
            }
        }
        catch (Exception ex) { KrnlLogger.Write($"SettingsControl.OnMcpToggled: {ex.Message}"); }
    }

    private void OnOpenLogsFolder(object sender, RoutedEventArgs e)
    {
        try
        {
            var logPath = KrnlLogger.GetLogPath();
            var dir = System.IO.Path.GetDirectoryName(logPath);
            if (dir is not null && System.IO.Directory.Exists(dir))
                Process.Start("explorer.exe", dir);
            else
                MessageBox.Show($"Log folder not found: {dir}", "Logs", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            MessageBox.Show($"Could not open logs folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
