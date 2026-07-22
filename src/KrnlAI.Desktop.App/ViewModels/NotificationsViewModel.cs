using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public class NotificationsViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private readonly ILogger<NotificationsViewModel> _logger;
    private List<NotificationItem>? _notifications;
    public List<NotificationItem>? Notifications { get => _notifications; set { SetProperty(ref _notifications, value); OnPropertyChanged(nameof(HasData)); OnPropertyChanged(nameof(HasNoData)); } }
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasData => Notifications?.Count > 0;
    public bool HasNoData => !IsLoading && !HasError && !HasData;
    public ICommand LoadCommand { get; }
    public ICommand MarkReadCommand { get; }

    public NotificationsViewModel(IKernelClient kernelClient, ILogger<NotificationsViewModel>? logger = null)
    {
        _kernelClient = kernelClient;
        _logger = logger ?? NullLogger<NotificationsViewModel>.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        MarkReadCommand = new AsyncRelayCommand(async p => { if (p is string id) await MarkReadAsync(id); });
    }

    public NotificationsViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadAsync()
    {
        IsLoading = true; ErrorMessage = "";
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local) { ErrorMessage = "Indisponivel no modo Local"; return; }
            Notifications = await _kernelClient.GetNotificationsAsync().ConfigureAwait(false);
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to load notifications"); ErrorMessage = ex.Message; }
        finally { IsLoading = false; }
    }

    public async Task MarkReadAsync(string id)
    {
        try { await _kernelClient.MarkNotificationReadAsync(id).ConfigureAwait(false); await LoadAsync().ConfigureAwait(false); }
        catch (Exception ex) { _logger.LogError(ex, "Failed to mark read {Id}", id); ErrorMessage = ex.Message; }
    }
}
