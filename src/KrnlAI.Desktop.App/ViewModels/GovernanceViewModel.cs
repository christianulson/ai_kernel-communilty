using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KrnlAI.Desktop.App.ViewModels;

public class GovernanceViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private readonly ILogger<GovernanceViewModel> _logger;
    private List<GovernanceBudget>? _budgets;
    public List<GovernanceBudget>? Budgets { get => _budgets; set { SetProperty(ref _budgets, value); OnPropertyChanged(nameof(HasData)); OnPropertyChanged(nameof(HasNoData)); } }
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasData => Budgets?.Count > 0;
    public bool HasNoData => !IsLoading && !HasError && !HasData;
    public ICommand LoadCommand { get; }

    public GovernanceViewModel(IKernelClient kernelClient, ILogger<GovernanceViewModel>? logger = null)
    {
        _kernelClient = kernelClient;
        _logger = logger ?? NullLogger<GovernanceViewModel>.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public GovernanceViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadAsync()
    {
        IsLoading = true; ErrorMessage = "";
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local) { ErrorMessage = "Indisponivel no modo Local"; return; }
            Budgets = await _kernelClient.GetAutonomyBudgetsAsync().ConfigureAwait(false);
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to load governance"); ErrorMessage = ex.Message; }
        finally { IsLoading = false; }
    }
}
