using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Infrastructure.Abstractions;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

/// <summary>View model for the admin users page, displaying and managing user accounts.</summary>
public sealed class AdminUsersViewModel : ViewModelBase
{
    private readonly ILogger<AdminUsersViewModel> _logger;
    public ObservableCollection<UserInfo> Users { get; } = new();
    private UserInfo? _selectedUser;
    public UserInfo? SelectedUser { get => _selectedUser; set { SetProperty(ref _selectedUser, value); OnPropertyChanged(nameof(HasSelection)); } }
    public bool HasSelection => _selectedUser != null;
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    private string _statusMessage = "";
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

    public ICommand LoadCommand { get; }
    public ICommand ActivateCommand { get; }
    public ICommand SuspendCommand { get; }

    public AdminUsersViewModel()
        : this(ServiceLocator.Instance.GetLogger<AdminUsersViewModel>()) { }

    public AdminUsersViewModel(ILogger<AdminUsersViewModel>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AdminUsersViewModel>.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        ActivateCommand = new AsyncRelayCommand(() => UpdateStatusAsync(true));
        SuspendCommand = new AsyncRelayCommand(() => UpdateStatusAsync(false));
    }

    public async Task LoadAsync()
    {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local) { StatusMessage = "Indisponível no modo Local"; return; }
        IsLoading = true;
        try
        {
            var api = ServiceLocator.Instance.AdminApi;
            if (api == null) { StatusMessage = "Admin API indisponível"; return; }

            var users = await api.GetUsersAsync();
            Users.Clear();
            foreach (var u in users) Users.Add(u);
            StatusMessage = $"{Users.Count} usuários carregados";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
            _logger.LogWarning(ex, "AdminUsersViewModel.LoadAsync failed");
        }
        finally { IsLoading = false; }
    }

    private async Task UpdateStatusAsync(bool isActive)
    {
        if (_selectedUser == null) return;
        try
        {
            var api = ServiceLocator.Instance.AdminApi;
            if (api == null) return;
            await api.UpdateUserStatusAsync(_selectedUser.Id, new UpdateStatusRequest(isActive));
            StatusMessage = $"Usuário {_selectedUser.Name} {(isActive ? "ativado" : "suspenso")}";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
            _logger.LogWarning(ex, "AdminUsersViewModel.UpdateStatusAsync failed");
        }
    }
}
