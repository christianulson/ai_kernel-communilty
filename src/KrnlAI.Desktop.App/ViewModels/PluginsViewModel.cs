using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public class PluginsViewModel : ViewModelBase
{
    private readonly IKernelClient _client;
    private readonly ILogger<PluginsViewModel> _logger;
    public ObservableCollection<McpServerInfo> Plugins { get; } = new();
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasNoData => !IsLoading && Plugins.Count == 0 && !HasError;
    public ICommand LoadCommand { get; }
    public ICommand TogglePluginCommand { get; }

    public PluginsViewModel() : this(ServiceLocator.Instance.KernelClient, ServiceLocator.Instance.GetLogger<PluginsViewModel>()) { }
    public PluginsViewModel(IKernelClient client, ILogger<PluginsViewModel>? logger = null)
    {
        _client = client; _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PluginsViewModel>.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        TogglePluginCommand = new AsyncRelayCommand(async p => { if (p is string id) await TogglePluginAsync(id); });
    }

    public async Task LoadAsync()
    {
        IsLoading = true; ErrorMessage = "";
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local) { ErrorMessage = "Indisponível no modo Local"; return; }
            var r = await _client.GetPluginsAsync();
            Plugins.Clear();
            if (r != null) foreach (var p in r) Plugins.Add(p);
            OnPropertyChanged(nameof(HasNoData));
        }
        catch (Exception ex) { ErrorMessage = $"Erro: {ex.Message}"; _logger.LogWarning(ex, "PluginsViewModel.LoadAsync failed"); }
        finally { IsLoading = false; }
    }

    private async Task TogglePluginAsync(string id)
    {
        try { await _client.ToggleMcpServerAsync(id, true); await LoadAsync(); }
        catch (Exception ex) { _logger.LogWarning(ex, "TogglePlugin failed"); }
    }
}
