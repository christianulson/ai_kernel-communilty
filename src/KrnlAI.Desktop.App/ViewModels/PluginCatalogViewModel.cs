using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public class PluginCatalogViewModel : ViewModelBase
{
    private readonly IKernelClient _client;
    private readonly ILogger<PluginCatalogViewModel> _logger;
    public ObservableCollection<McpServerInfo> Plugins { get; } = [];
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public ICommand RefreshCommand { get; }

    public PluginCatalogViewModel() : this(ServiceLocator.Instance.KernelClient, ServiceLocator.Instance.GetLogger<PluginCatalogViewModel>()) { }
    public PluginCatalogViewModel(IKernelClient client, ILogger<PluginCatalogViewModel>? logger = null)
    {
        _client = client;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PluginCatalogViewModel>.Instance;
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local) return;
            var r = await _client.GetPluginsAsync();
            Plugins.Clear();
            if (r != null) foreach (var p in r) Plugins.Add(p);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "PluginCatalogViewModel.LoadAsync failed"); }
        finally { IsLoading = false; }
    }
}
