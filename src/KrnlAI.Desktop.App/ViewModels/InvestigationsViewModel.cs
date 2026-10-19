using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

/// <summary>View model for the investigations list page, displaying agent investigation cases.</summary>
public class InvestigationsViewModel : ViewModelBase
{
    private readonly IKernelClient _client;
    private readonly ILogger<InvestigationsViewModel> _logger;
    public ObservableCollection<InvestigationInfo> Investigations { get; } = [];
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasNoData => !IsLoading && Investigations.Count == 0 && !HasError;
    public ICommand LoadCommand { get; }

    public InvestigationsViewModel() : this(ServiceLocator.Instance.KernelClient, ServiceLocator.Instance.GetLogger<InvestigationsViewModel>()) { }
    public InvestigationsViewModel(IKernelClient client, ILogger<InvestigationsViewModel>? logger = null)
    {
        _client = client;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<InvestigationsViewModel>.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

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
            var r = await _client.GetInvestigationsAsync();
            Investigations.Clear();
            if (r != null) { foreach (var i in r) Investigations.Add(i); }
            OnPropertyChanged(nameof(HasNoData));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar investigações: {ex.Message}";
            _logger.LogWarning(ex, "InvestigationsViewModel.LoadAsync failed");
        }
        finally { IsLoading = false; }
    }
}
