using System.Net.Http;
using System.Net.Http.Json;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Desktop.Core.Services;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public sealed class SidecarViewModel : ViewModelBase
{
    private readonly ILogger<SidecarViewModel> _logger;
    private readonly HttpClient _http;
    private readonly ISettingsService _settings;
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
        var baseUrl = Environment.GetEnvironmentVariable("KRNL__API_BASE_URL") ?? "http://localhost:5235";
        _http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(10) };
        _settings = ServiceLocator.Instance.SettingsService;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync);
        SaveConfigCommand = new AsyncRelayCommand(SaveConfigAsync);
        ResetToCommunityCommand = new AsyncRelayCommand(ResetToCommunityAsync);
    }

    public string Mode
    {
        get => _mode;
        set { if (SetProperty(ref _mode, value)) { OnPropertyChanged(nameof(ModeLabel)); OnPropertyChanged(nameof(ModeDescription)); } }
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
            var s = _settings.LoadSettings();
            var sidecarSection = s.SidecarConfig;
            if (sidecarSection != null)
            {
                Mode = sidecarSection.Mode ?? "community";
                AuthEndpoint = sidecarSection.AuthEndpoint ?? "";
                GatewayEndpoint = sidecarSection.GatewayEndpoint ?? "";
                ApiKey = sidecarSection.ApiKey ?? "";
                TenantId = sidecarSection.TenantId ?? "";
            }
            StatusMessage = "Configuração carregada.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load sidecar diagnostics");
            StatusMessage = "Erro ao carregar";
        }
    }

    private async Task TestConnectionAsync()
    {
        try
        {
            StatusMessage = "Testando...";
            ErrorMessage = "";

            if (ServiceLocator.Instance.CurrentMode == RunMode.Local)
            {
                StatusMessage = "Modo local — sidecar não disponível";
                return;
            }

            if (_mode == "enterprise" && !string.IsNullOrWhiteSpace(_gatewayEndpoint))
            {
                using var testClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var response = await testClient.GetAsync(_gatewayEndpoint.TrimEnd('/') + "/health");
                StatusMessage = response.IsSuccessStatusCode ? "Conexão OK" : $"Falha: HTTP {(int)response.StatusCode}";
            }
            else
            {
                var response = await _http.GetAsync("/health");
                StatusMessage = response.IsSuccessStatusCode ? "Conexão OK" : $"Falha: HTTP {(int)response.StatusCode}";
            }
        }
        catch (TaskCanceledException)
        {
            ErrorMessage = "Timeout de conexão";
            StatusMessage = "Falha";
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = $"Erro de conexão: {ex.Message}";
            StatusMessage = "Falha";
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
            var s = _settings.LoadSettings();
            _settings.SaveSettings(s with
            {
                SidecarConfig = new SidecarSettings(
                    Mode,
                    AuthEndpoint,
                    GatewayEndpoint,
                    ApiKey,
                    TenantId)
            });
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
            await SaveConfigAsync();
            StatusMessage = "Resetado para modo community.";
            ErrorMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao resetar: {ex.Message}";
        }
    }
}
