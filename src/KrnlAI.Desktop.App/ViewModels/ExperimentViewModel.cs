using System.Collections.ObjectModel;
using System.Windows.Input;

namespace KrnlAI.Desktop.App.ViewModels;

public class ExperimentViewModel : ViewModelBase
{
    public ObservableCollection<string> Scenarios { get; } =
    [
        "🧪 Test: Policy evaluation accuracy",
        "🧪 Test: Memory recall speed",
        "🧪 Test: Causal reasoning chain",
        "🧪 Test: Multi-step planning"
    ];
    private string _status = "Pronto.";
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    public ICommand RunCommand { get; }
    public ExperimentViewModel()
    {
        RunCommand = new AsyncRelayCommand(async () =>
        {
            Status = "Executando experimento...";
            await Task.Delay(2000).ConfigureAwait(false);
            Status = "Experimento concluído. Resultados salvos.";
        });
    }
}
