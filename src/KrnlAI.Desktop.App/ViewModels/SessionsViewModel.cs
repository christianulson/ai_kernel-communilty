using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public class SessionsViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private readonly ILogger<SessionsViewModel> _logger;
    public ObservableCollection<SessionShare> Shares { get; } = [];
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasNoData => !IsLoading && Shares.Count == 0 && !HasError;
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public ICommand LoadCommand { get; }

    public SessionsViewModel(IKernelClient kernelClient, ILogger<SessionsViewModel>? logger = null)
    {
        _kernelClient = kernelClient;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SessionsViewModel>.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public SessionsViewModel() : this(ServiceLocator.Instance.KernelClient) { }

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
            var resp = await _kernelClient.GetSharesAsync();
            Shares.Clear();
            if (resp?.Shares != null)
                foreach (var s in resp.Shares) Shares.Add(s);
            OnPropertyChanged(nameof(HasNoData));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar sessões: {ex.Message}";
            _logger.LogWarning(ex, "SessionsViewModel.LoadAsync failed");
        }
        finally { IsLoading = false; }
    }
}
