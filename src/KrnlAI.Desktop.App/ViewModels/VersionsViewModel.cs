using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public class VersionsViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private readonly ILogger<VersionsViewModel> _logger;
    public ObservableCollection<ContractEntry> Contracts { get; } = [];
    private VersionsInfo? _versions;
    public VersionsInfo? Versions { get => _versions; set => SetProperty(ref _versions, value); }
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasNoData => !IsLoading && Versions == null && !HasError;
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public ICommand LoadCommand { get; }

    public VersionsViewModel(IKernelClient kernelClient, ILogger<VersionsViewModel>? logger = null)
    {
        _kernelClient = kernelClient;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<VersionsViewModel>.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public VersionsViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local)
            {
                ErrorMessage = "Indisponível no modo Local";
                return;
            }
            Versions = await _kernelClient.GetVersionsAsync();
            var contractsResp = await _kernelClient.GetContractsAsync();
            Contracts.Clear();
            if (contractsResp?.Contracts != null)
                foreach (var c in contractsResp.Contracts) Contracts.Add(c);
            OnPropertyChanged(nameof(HasNoData));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar versões: {ex.Message}";
            _logger.LogWarning(ex, "VersionsViewModel.LoadAsync failed");
        }
        finally { IsLoading = false; }
    }
}
