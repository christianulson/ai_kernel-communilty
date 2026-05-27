using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public sealed class PrivacyDashboardViewModel : ViewModelBase
{
    private readonly ITelemetryPrivacyService _service;
    private readonly ILogger<PrivacyDashboardViewModel> _logger;
    private bool _isBusy;
    private TelemetryConsentLevel _selectedConsentLevel;
    private string _consentTitle = "Não coletar";
    private string _consentDescription = "A telemetria está desabilitada.";
    private string _statusMessage = "Pronto.";
    private DateTimeOffset? _grantedAt;
    private DateTimeOffset? _revokedAt;

    public PrivacyDashboardViewModel()
        : this(ServiceLocator.Instance.TelemetryPrivacyService, ServiceLocator.Instance.GetLogger<PrivacyDashboardViewModel>())
    {
    }

    public PrivacyDashboardViewModel(ITelemetryPrivacyService service, ILogger<PrivacyDashboardViewModel>? logger = null)
    {
        _service = service;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PrivacyDashboardViewModel>.Instance;

        RefreshCommand = new AsyncRelayCommand(() => LoadAsync());
        SaveConsentCommand = new AsyncRelayCommand(() => SaveAsync());
        RequestExportCommand = new AsyncRelayCommand(() => RequestExportAsync());
        RequestDeletionCommand = new AsyncRelayCommand(() => RequestDeletionAsync());
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public TelemetryConsentLevel SelectedConsentLevel
    {
        get => _selectedConsentLevel;
        set
        {
            if (SetProperty(ref _selectedConsentLevel, value))
                UpdateDescription(value);
        }
    }

    public string ConsentTitle
    {
        get => _consentTitle;
        private set => SetProperty(ref _consentTitle, value);
    }

    public string ConsentDescription
    {
        get => _consentDescription;
        private set => SetProperty(ref _consentDescription, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public DateTimeOffset? GrantedAt
    {
        get => _grantedAt;
        private set => SetProperty(ref _grantedAt, value);
    }

    public DateTimeOffset? RevokedAt
    {
        get => _revokedAt;
        private set => SetProperty(ref _revokedAt, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand SaveConsentCommand { get; }
    public ICommand RequestExportCommand { get; }
    public ICommand RequestDeletionCommand { get; }

    public Task LoadAsync(CancellationToken ct = default)
        => ExecuteBusyAsync(async () =>
        {
            var state = await _service.GetConsentAsync(ct).ConfigureAwait(false);
            ApplyState(state);
            StatusMessage = "Consentimento carregado.";
        });

    public Task SaveAsync(CancellationToken ct = default)
        => ExecuteBusyAsync(async () =>
        {
            var state = await _service.SetConsentAsync(SelectedConsentLevel, ct).ConfigureAwait(false);
            ApplyState(state);
            StatusMessage = "Consentimento atualizado.";
        });

    public Task RequestExportAsync(CancellationToken ct = default)
        => ExecuteBusyAsync(async () =>
        {
            var result = await _service.RequestExportAsync(ct).ConfigureAwait(false);
            StatusMessage = result.Message;
        });

    public Task RequestDeletionAsync(CancellationToken ct = default)
        => ExecuteBusyAsync(async () =>
        {
            var result = await _service.RequestDeletionAsync(ct).ConfigureAwait(false);
            StatusMessage = result.Message;
        });

    private void ApplyState(TelemetryPrivacyState state)
    {
        SelectedConsentLevel = state.ConsentLevel;
        ConsentTitle = state.Title;
        ConsentDescription = state.Description;
        GrantedAt = state.GrantedAt;
        RevokedAt = state.RevokedAt;
    }

    private void UpdateDescription(TelemetryConsentLevel level)
    {
        ConsentTitle = level switch
        {
            TelemetryConsentLevel.None => "Não coletar",
            TelemetryConsentLevel.Anonymous => "Coleta anônima",
            TelemetryConsentLevel.Full => "Coleta completa",
            _ => "Telemetria"
        };

        ConsentDescription = level switch
        {
            TelemetryConsentLevel.None => "Nenhum dado de telemetria será enviado.",
            TelemetryConsentLevel.Anonymous => "Apenas dados anonimizados serão enviados.",
            TelemetryConsentLevel.Full => "Dados completos com hash de usuário serão enviados.",
            _ => "Telemetria"
        };
    }

    private async Task ExecuteBusyAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telemetry privacy operation failed");
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
