using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public class ApprovalViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private readonly ILogger<ApprovalViewModel> _logger;
    private List<ApprovalRequest>? _approvals;
    public List<ApprovalRequest>? Approvals { get => _approvals; set { SetProperty(ref _approvals, value); OnPropertyChanged(nameof(HasData)); OnPropertyChanged(nameof(HasNoData)); } }
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasData => Approvals?.Count > 0;
    public bool HasNoData => !IsLoading && !HasError && !HasData;
    public ICommand LoadCommand { get; }
    public ICommand ApproveCommand { get; }
    public ICommand RejectCommand { get; }

    public ApprovalViewModel(IKernelClient kernelClient, ILogger<ApprovalViewModel>? logger = null)
    {
        _kernelClient = kernelClient;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ApprovalViewModel>.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        ApproveCommand = new AsyncRelayCommand<string>(ApproveAsync);
        RejectCommand = new AsyncRelayCommand<string>(RejectAsync);
    }

    public ApprovalViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local)
            {
                ErrorMessage = "Indisponivel no modo Local";
                return;
            }
            Approvals = await _kernelClient.GetPendingApprovalsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar aprovacoes: {ex.Message}";
            _logger.LogWarning(ex, "ApprovalViewModel.LoadAsync failed");
        }
        finally { IsLoading = false; }
    }

    private async Task ApproveAsync(string requestId)
    {
        try
        {
            await _kernelClient.ApproveRequestAsync(requestId);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao aprovar: {ex.Message}";
            _logger.LogWarning(ex, "ApprovalViewModel.ApproveAsync failed");
        }
    }

    private async Task RejectAsync(string requestId)
    {
        try
        {
            await _kernelClient.RejectRequestAsync(requestId);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao rejeitar: {ex.Message}";
            _logger.LogWarning(ex, "ApprovalViewModel.RejectAsync failed");
        }
    }
}
