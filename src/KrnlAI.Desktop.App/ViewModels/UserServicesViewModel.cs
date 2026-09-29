using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public sealed class UserServicesViewModel : ViewModelBase
{
    private readonly IKernelClient? _kernelClient;
    private readonly ILogger<UserServicesViewModel> _logger;
    private bool _isLoading;
    private string _statusMessage = "";

    public ObservableCollection<UserServiceInfo> Services { get; } = [];

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand LoadCommand { get; }
    public ICommand ToggleCommand { get; }

    public UserServicesViewModel()
        : this(ServiceLocator.Instance.KernelClient, ServiceLocator.Instance.GetLogger<UserServicesViewModel>())
    {
    }

    public UserServicesViewModel(IKernelClient? kernelClient, ILogger<UserServicesViewModel>? logger = null)
    {
        _kernelClient = kernelClient;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<UserServicesViewModel>.Instance;

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        ToggleCommand = new AsyncRelayCommand(async (param) =>
        {
            if (param is not string serviceType || string.IsNullOrWhiteSpace(serviceType)) return;
            ToggleService(serviceType);
            await Task.CompletedTask;
        });
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var result = _kernelClient != null
                ? await _kernelClient.GetUserServicesAsync()
                : null;

            Services.Clear();
            if (result is { Count: > 0 })
            {
                foreach (var s in result)
                    Services.Add(s);
                StatusMessage = $"{Services.Count} serviço(s) carregado(s)";
            }
            else
            {
                LoadDemoServices();
                StatusMessage = "Usando dados de demonstração";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UserServicesViewModel.LoadAsync failed");
            LoadDemoServices();
            StatusMessage = $"Erro ao carregar: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ToggleService(string serviceType)
    {
        var idx = -1;
        for (var i = 0; i < Services.Count; i++)
        {
            if (Services[i].ServiceType == serviceType)
            {
                idx = i;
                break;
            }
        }
        if (idx < 0) return;

        var current = Services[idx];
        Services[idx] = current with { Enabled = !current.Enabled };
        StatusMessage = $"Serviço {current.ServiceType} {(current.Enabled ? "desativado" : "ativado")}";
    }

    private void LoadDemoServices()
    {
        Services.Clear();
        Services.Add(new UserServiceInfo("github", true, true, DateTimeOffset.UtcNow.AddDays(-2)));
        Services.Add(new UserServiceInfo("slack", true, false, DateTimeOffset.UtcNow.AddMonths(-1)));
        Services.Add(new UserServiceInfo("gmail", false, false, null));
        Services.Add(new UserServiceInfo("demo", true, true, DateTimeOffset.UtcNow));
    }
}
