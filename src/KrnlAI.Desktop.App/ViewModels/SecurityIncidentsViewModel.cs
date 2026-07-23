using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KrnlAI.Desktop.App.ViewModels;

public class SecurityIncidentsViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private readonly ILogger<SecurityIncidentsViewModel> _logger;
    private List<SecurityIncident>? _incidents;
    public List<SecurityIncident>? Incidents { get => _incidents; set { SetProperty(ref _incidents, value); OnPropertyChanged(nameof(HasData)); OnPropertyChanged(nameof(HasNoData)); } }
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasData => Incidents?.Count > 0;
    public bool HasNoData => !IsLoading && !HasError && !HasData;
    public ICommand LoadCommand { get; }
    public ICommand ResolveCommand { get; }

    public SecurityIncidentsViewModel(IKernelClient kernelClient, ILogger<SecurityIncidentsViewModel>? logger = null)
    {
        _kernelClient = kernelClient;
        _logger = logger ?? NullLogger<SecurityIncidentsViewModel>.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        ResolveCommand = new AsyncRelayCommand(async p => { if (p is string id) await ResolveAsync(id); });
    }

    public SecurityIncidentsViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local) { ErrorMessage = "Indisponivel no modo Local"; return; }
            Incidents = await _kernelClient.GetSecurityIncidentsAsync().ConfigureAwait(false);
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to load incidents"); ErrorMessage = ex.Message; }
        finally { IsLoading = false; }
    }

    public async Task ResolveAsync(string id)
    {
        try
        {
            await _kernelClient.ResolveSecurityAlertAsync(id).ConfigureAwait(false);
            await LoadAsync().ConfigureAwait(false);
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to resolve {Id}", id); ErrorMessage = ex.Message; }
    }
}
