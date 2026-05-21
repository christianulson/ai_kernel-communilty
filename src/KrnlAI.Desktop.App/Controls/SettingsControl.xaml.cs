using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using KrnlAI.Desktop.Core.Services;
namespace KrnlAI.Desktop.App.Controls;
public partial class SettingsControl : UserControl
{
    public SettingsControl() { InitializeComponent(); }

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
