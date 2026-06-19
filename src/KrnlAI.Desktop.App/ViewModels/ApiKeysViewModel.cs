using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

/// <summary>
/// View model for the desktop API keys management screen.
/// </summary>
public sealed class ApiKeysViewModel : ViewModelBase
{
    private readonly IApiKeyManagementService _service;
    private readonly ILogger<ApiKeysViewModel> _logger;
    private bool _isBusy;
    private string _statusMessage = "Pronto.";
    private string _errorMessage = string.Empty;
    private string _nameInput = string.Empty;
    private int _ttlDaysInput = 30;
    private ApiKeyScope _selectedScope = ApiKeyScope.ReadWrite;
    private string? _createdFullKey;
    private string? _createdName;
    private string? _createdWarning;
    private DateTimeOffset? _createdExpiresAt;
    private ApiKeyUsageSummary _summary = new(0, 0, 0, 0, null);

    public ApiKeysViewModel()
        : this(ServiceLocator.Instance.ApiKeyManagementService, ServiceLocator.Instance.GetLogger<ApiKeysViewModel>())
    {
    }

    public ApiKeysViewModel(IApiKeyManagementService service, ILogger<ApiKeysViewModel>? logger = null)
    {
        _service = service;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ApiKeysViewModel>.Instance;

        LoadCommand = new AsyncRelayCommand(() => LoadAsync());
        CreateCommand = new AsyncRelayCommand(() => CreateAsync());
        RefreshCommand = new AsyncRelayCommand(() => LoadAsync());
    }

    public ObservableCollection<ApiKeyListItem> Keys { get; } = [];

    public IEnumerable<ApiKeyScope> AvailableScopes { get; } = Enum.GetValues<ApiKeyScope>();

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string NameInput
    {
        get => _nameInput;
        set => SetProperty(ref _nameInput, value);
    }

    public int TtlDaysInput
    {
        get => _ttlDaysInput;
        set => SetProperty(ref _ttlDaysInput, value);
    }

    public ApiKeyScope SelectedScope
    {
        get => _selectedScope;
        set => SetProperty(ref _selectedScope, value);
    }

    public string? CreatedFullKey
    {
        get => _createdFullKey;
        private set => SetProperty(ref _createdFullKey, value);
    }

    public string? CreatedName
    {
        get => _createdName;
        private set => SetProperty(ref _createdName, value);
    }

    public string? CreatedWarning
    {
        get => _createdWarning;
        private set => SetProperty(ref _createdWarning, value);
    }

    public DateTimeOffset? CreatedExpiresAt
    {
        get => _createdExpiresAt;
        private set => SetProperty(ref _createdExpiresAt, value);
    }

    public ApiKeyUsageSummary Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    public ICommand LoadCommand { get; }
    public ICommand CreateCommand { get; }
    public ICommand RefreshCommand { get; }

    public Task LoadAsync(CancellationToken ct = default)
        => ExecuteBusyAsync(async () =>
        {
            await ReloadAsync(ct).ConfigureAwait(false);
            StatusMessage = BuildStatusMessage(Summary);
            ErrorMessage = string.Empty;
        });

    public Task CreateAsync(CancellationToken ct = default)
        => ExecuteBusyAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(NameInput))
                throw new InvalidOperationException("Nome da chave é obrigatório.");

            var ttl = TimeSpan.FromDays(TtlDaysInput <= 0 ? 30 : TtlDaysInput);
            var created = await _service.CreateAsync(new ApiKeyCreationRequest(NameInput.Trim(), ttl, SelectedScope), ct).ConfigureAwait(false);
            CreatedFullKey = created.FullKey;
            CreatedName = created.Name;
            CreatedWarning = created.Warning;
            CreatedExpiresAt = created.ExpiresAt;
            await ReloadAsync(ct).ConfigureAwait(false);
            StatusMessage = "Chave criada. Copie o valor agora.";
            ErrorMessage = string.Empty;
        });

    public Task RevokeAsync(string keyId, CancellationToken ct = default)
        => ExecuteBusyAsync(async () =>
        {
            await _service.RevokeAsync(keyId, ct).ConfigureAwait(false);
            await ReloadAsync(ct).ConfigureAwait(false);
            StatusMessage = "Chave revogada.";
            ErrorMessage = string.Empty;
        });

    public void ClearCreatedKey()
    {
        CreatedFullKey = null;
        CreatedName = null;
        CreatedWarning = null;
        CreatedExpiresAt = null;
    }

    private static string BuildStatusMessage(ApiKeyUsageSummary summary)
    {
        if (summary.Total == 0)
            return "Nenhuma chave cadastrada.";

        return summary.Active switch
        {
            0 => "Nenhuma chave ativa.",
            1 => "1 chave ativa.",
            _ => $"{summary.Active} chaves ativas."
        };
    }

    private async Task ReloadAsync(CancellationToken ct)
    {
        var keys = await _service.ListAsync(ct).ConfigureAwait(false);
        Keys.Clear();
        foreach (var item in keys)
            Keys.Add(item);

        Summary = await _service.GetStatsAsync(ct).ConfigureAwait(false);
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
            _logger.LogError(ex, "API key management operation failed");
            ErrorMessage = ex.Message;
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
