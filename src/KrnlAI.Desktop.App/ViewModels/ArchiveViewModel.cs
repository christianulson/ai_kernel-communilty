using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public class ArchiveViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private readonly ILogger<ArchiveViewModel> _logger;
    private ArchiveStats? _stats;
    public ArchiveStats? Stats { get => _stats; set => SetProperty(ref _stats, value); }
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasNoData => !IsLoading && Stats == null && !HasError;
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public ICommand LoadCommand { get; }

    public ArchiveViewModel(IKernelClient kernelClient, ILogger<ArchiveViewModel>? logger = null)
    {
        _kernelClient = kernelClient;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ArchiveViewModel>.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public ArchiveViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local)
            {
                ErrorMessage = "Indisponível no modo Local";
                return;
            }
            Stats = await _kernelClient.GetArchiveStatsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar archive: {ex.Message}";
            _logger.LogWarning(ex, "ArchiveViewModel.LoadAsync failed");
        }
        finally { IsLoading = false; }
    }
}
