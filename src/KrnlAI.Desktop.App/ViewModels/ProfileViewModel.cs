using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class ProfileViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private string _userId = "ui_user", _name = "", _email = "", _role = "", _errorMessage = "";
    public string UserId { get => _userId; set => SetProperty(ref _userId, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string Role { get => _role; set => SetProperty(ref _role, value); }
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    private bool _isSaving;
    public bool IsSaving { get => _isSaving; set { SetProperty(ref _isSaving, value); OnPropertyChanged(nameof(SaveButtonText)); } }
    public string SaveButtonText => IsSaving ? "Salvando..." : "Salvar";

    public ICommand LoadCommand { get; }
    public ICommand SaveCommand { get; }

    public ProfileViewModel(IKernelClient kernelClient)
    {
        _kernelClient = kernelClient;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    public ProfileViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadAsync()
    {
        if (string.IsNullOrWhiteSpace(UserId)) return;
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local)
            {
                ErrorMessage = "Indisponível no modo Local";
                return;
            }
            var profile = await _kernelClient.GetUserProfileAsync(UserId).ConfigureAwait(false);
            if (profile != null)
            {
                Name = profile.Name ?? "";
                Email = profile.Email ?? "";
                Role = profile.Role ?? "";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar perfil: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(UserId)) return;
        if (ServiceLocator.Instance.CurrentMode == RunMode.Local)
        {
            ErrorMessage = "Indisponível no modo Local";
            return;
        }
        IsSaving = true;
        ErrorMessage = "";
        try
        {
            var profile = new UserProfile(UserId, Name, Email, Role, null, DateTime.UtcNow);
            var success = await _kernelClient.UpdateUserProfileAsync(profile).ConfigureAwait(false);
            if (!success)
                ErrorMessage = "Falha ao salvar perfil";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao salvar perfil: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }
}
