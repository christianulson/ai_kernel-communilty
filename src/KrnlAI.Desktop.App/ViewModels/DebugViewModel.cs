using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;

namespace KrnlAI.Desktop.App.ViewModels;

public class DebugViewModel : ViewModelBase
{
    public ObservableCollection<string> Diagnostics { get; } = [];
    private string _status = "Pronto.";
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    public ICommand RunDiagnosticsCommand { get; }

    public DebugViewModel()
    {
        RunDiagnosticsCommand = new AsyncRelayCommand(async () =>
        {
            Status = "Executando diagnósticos...";
            Diagnostics.Clear();
            Diagnostics.Add("🧠 Kernel: " + (ServiceLocator.Instance.KernelClient != null ? "OK" : "N/A"));
            Diagnostics.Add("🔧 Embedded: " + (ServiceLocator.Instance.EmbeddedKernel != null ? "OK" : "N/A"));
            Diagnostics.Add("⚙️ Modo: " + ServiceLocator.Instance.CurrentMode);
            Diagnostics.Add("🔗 API: " + ServiceLocator.Instance.SettingsService.LoadSettings().ApiEndpoint);
            try
            {
                var client = ServiceLocator.Instance.KernelClient;
                var healthy = client != null && await client.CheckHealthAsync().ConfigureAwait(false);
                Diagnostics.Add($"❤️ Health: {(healthy ? "OK" : "N/A")}");
            }
            catch (Exception ex) { Diagnostics.Add($"❤️ Health: FAIL ({ex.Message})"); }
            Status = $"Diagnóstico concluído. {Diagnostics.Count} verificações.";
        });
    }
}
