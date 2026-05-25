using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class InvestigationsViewModel : ViewModelBase
{
    private readonly IKernelClient _client;
    public ObservableCollection<InvestigationInfo> Investigations { get; } = new();
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasNoData => !IsLoading && Investigations.Count == 0 && !HasError;
    public ICommand LoadCommand { get; }

    public InvestigationsViewModel() : this(ServiceLocator.Instance.KernelClient) { }
    public InvestigationsViewModel(IKernelClient client) { _client = client; LoadCommand = new AsyncRelayCommand(LoadAsync); }

    public async Task LoadAsync()
    {
        IsLoading = true; ErrorMessage = "";
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local)
            {
                ErrorMessage = "Indisponível no modo Local";
                return;
            }
            var r = await _client.GetInvestigationsAsync(); Investigations.Clear(); foreach (var i in r) Investigations.Add(i); OnPropertyChanged(nameof(HasNoData));
        }
        catch (Exception ex) { ErrorMessage = $"Erro ao carregar investigações: {ex.Message}"; }
        finally { IsLoading = false; }
    }
}
