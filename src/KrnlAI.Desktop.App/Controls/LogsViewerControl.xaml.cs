using System.IO;
using System.Windows.Controls;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Controls;

public sealed partial class LogsViewerControl : UserControl
{
    public LogsViewerControl() { InitializeComponent(); }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e) => LoadLogs();

    private void OnRefresh(object sender, System.Windows.RoutedEventArgs e) => LoadLogs();

    private void LoadLogs()
    {
        try
        {
            var logPath = KrnlLogger.GetLogPath();
            if (File.Exists(logPath))
            {
                var content = File.ReadAllText(logPath);
                var lines = content.Split('\n');
                LogsText.Text = string.Join("\n", lines.TakeLast(200));
            }
            else LogsText.Text = "Nenhum log encontrado em: " + (logPath ?? "?");
        }
        catch (System.Exception ex) { LogsText.Text = $"Erro ao carregar logs: {ex.Message}"; }
    }
}
