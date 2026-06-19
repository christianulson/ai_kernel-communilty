using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Infrastructure.Abstractions;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

/// <summary>View model for the admin configuration page, displaying feature flags and server config.</summary>
public sealed class AdminConfigViewModel : ViewModelBase
{
    private readonly ILogger<AdminConfigViewModel> _logger;
    public ObservableCollection<FeatureFlag> FeatureFlags { get; } = [];
    public ObservableCollection<ConfigEntry> ConfigEntries { get; } = [];
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    private string _statusMessage = "";
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

    public ICommand LoadCommand { get; }

    public AdminConfigViewModel()
        : this(ServiceLocator.Instance.GetLogger<AdminConfigViewModel>()) { }

    public AdminConfigViewModel(ILogger<AdminConfigViewModel>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AdminConfigViewModel>.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public async Task LoadAsync()
    {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local) { StatusMessage = "Indisponível no modo Local"; return; }
        IsLoading = true;
        try
        {
            var api = ServiceLocator.Instance.AdminApi;
            if (api == null) { StatusMessage = "Admin API indisponível"; return; }

            var flags = await api.GetFeatureFlagsAsync();
            FeatureFlags.Clear();
            foreach (var f in flags) FeatureFlags.Add(f);

            var config = await api.GetConfigAsync();
            ConfigEntries.Clear();
            foreach (var c in config) ConfigEntries.Add(c);

            StatusMessage = $"Carregado: {FeatureFlags.Count} flags, {ConfigEntries.Count} configs";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
            _logger.LogWarning(ex, "AdminConfigViewModel.LoadAsync failed");
        }
        finally { IsLoading = false; }
    }

}
