using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class McpConfigViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private string _errorMessage = "";
    private bool _isLoading;
    private McpServerInfo? _selectedServer;

    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public McpServerInfo? SelectedServer { get => _selectedServer; set => SetProperty(ref _selectedServer, value); }

    public ObservableCollection<McpServerInfo> Servers { get; } = [];

    public ICommand LoadServersCommand { get; }
    public ICommand ToggleServerCommand { get; }
    public ICommand ClearErrorCommand { get; }

    public McpConfigViewModel(IKernelClient kernelClient)
    {
        _kernelClient = kernelClient;
        LoadServersCommand = new AsyncRelayCommand(async _ => await LoadServersAsync());
        ToggleServerCommand = new AsyncRelayCommand(async param =>
        {
            if (param is object[] args && args.Length == 2 && args[0] is string serverId && args[1] is bool enabled)
                await ToggleServerAsync(serverId, enabled);
        });
        ClearErrorCommand = new RelayCommand(_ => ErrorMessage = "");
    }

    public McpConfigViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadServersAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var servers = await _kernelClient.GetMcpServersAsync();
            Servers.Clear();
            foreach (var s in servers) Servers.Add(s);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar servidores: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> ToggleServerAsync(string serverId, bool enabled)
    {
        ErrorMessage = "";
        try
        {
            return await _kernelClient.ToggleMcpServerAsync(serverId, enabled);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao alternar servidor: {ex.Message}";
            return false;
        }
    }

    public async Task<bool> UpdateServerAsync(string serverId, McpServerConfig config)
    {
        ErrorMessage = "";
        try
        {
            return await _kernelClient.UpdateMcpServerAsync(serverId, config);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao atualizar servidor: {ex.Message}";
            return false;
        }
    }

    public void ClearError() => ErrorMessage = "";
}
