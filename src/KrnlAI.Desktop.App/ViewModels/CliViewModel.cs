using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;

namespace KrnlAI.Desktop.App.ViewModels;

public class CliViewModel : ViewModelBase
{
    private readonly CliBridgeService _cli = new();
    private string _command = "", _output = "", _statusMessage = "Pronto.";
    public ObservableCollection<string> History { get; } = new();
    public string Command { get => _command; set => SetProperty(ref _command, value); }
    public string Output { get => _output; set => SetProperty(ref _output, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public ICommand ExecuteCommand { get; }
    public ICommand ClearOutputCommand { get; }

    public string[] AvailableCommands { get; } = [
        "status", "health", "memory search <query>", "episodes list",
        "goals list", "moments list", "snapshots list", "objectives list",
        "investigations list", "policies list", "plugins list", "config list",
        "experiment run <name>", "benchmark run", "safety check",
        "upgrade check", "export data", "session list"
    ];

    public CliViewModel()
    {
        ExecuteCommand = new AsyncRelayCommand(async () =>
        {
            if (string.IsNullOrWhiteSpace(_command)) return;
            StatusMessage = "Executando...";
            History.Insert(0, $"> {_command}");
            Output = await _cli.ExecuteAsync(_command);
            StatusMessage = "Concluído.";
        });
        ClearOutputCommand = new RelayCommand(() => { Output = ""; History.Clear(); });
    }
}
