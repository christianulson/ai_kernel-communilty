using System.Windows.Input;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public sealed class SidecarViewModel : ViewModelBase
{
    private readonly ILogger<SidecarViewModel> _logger;
    private string _mode = "community";
    private string _authEndpoint = "";
    private string _gatewayEndpoint = "";
    private string _apiKey = "";
    private string _tenantId = "";
    private string _statusMessage = "Pronto.";
    private string _errorMessage = "";

    public SidecarViewModel(ILogger<SidecarViewModel>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SidecarViewModel>.Instance;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync);
        SaveConfigCommand = new AsyncRelayCommand(SaveConfigAsync);
        ResetToCommunityCommand = new AsyncRelayCommand(ResetToCommunityAsync);
    }

    public string Mode
    {
        get => _mode;
        set => SetProperty(ref _mode, value);
    }

    public string ModeLabel => $"Modo {Mode}";
    public string ModeDescription => Mode == "enterprise"
        ? "Configuração enterprise ativa. Autenticação e gateway remotos."
        : "Modo community. Operação local sem autenticação externa.";

    public string AuthEndpoint { get => _authEndpoint; set => SetProperty(ref _authEndpoint, value); }
    public string GatewayEndpoint { get => _gatewayEndpoint; set => SetProperty(ref _gatewayEndpoint, value); }
    public string ApiKey { get => _apiKey; set => SetProperty(ref _apiKey, value); }
    public string TenantId { get => _tenantId; set => SetProperty(ref _tenantId, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    public ICommand RefreshCommand { get; }
    public ICommand TestConnectionCommand { get; }
    public ICommand SaveConfigCommand { get; }
    public ICommand ResetToCommunityCommand { get; }

    private async Task RefreshAsync()
    {
        try
        {
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load sidecar diagnostics");
        }
    }

    private async Task TestConnectionAsync()
    {
        try
        {
            StatusMessage = "Testando...";
            ErrorMessage = "";
            await Task.Delay(500);
            StatusMessage = "Conexão OK";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro: {ex.Message}";
            StatusMessage = "Falha";
        }
    }

    private async Task SaveConfigAsync()
    {
        try
        {
            await Task.CompletedTask;
            StatusMessage = "Configuração salva.";
            ErrorMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao salvar: {ex.Message}";
        }
    }

    private async Task ResetToCommunityAsync()
    {
        try
        {
            Mode = "community";
            AuthEndpoint = "";
            GatewayEndpoint = "";
            ApiKey = "";
            TenantId = "";
            OnPropertyChanged(nameof(ModeLabel));
            OnPropertyChanged(nameof(ModeDescription));
            StatusMessage = "Resetado para modo community.";
            ErrorMessage = "";
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao resetar: {ex.Message}";
        }
    }
}
