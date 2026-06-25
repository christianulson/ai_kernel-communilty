using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class SelfImprovementViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private string _errorMessage = "";
    private bool _isLoading;
    private SelfImprovementStatus? _status;

    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public SelfImprovementStatus? Status { get => _status; set => SetProperty(ref _status, value); }

    public ICommand LoadStatusCommand { get; }
    public ICommand ClearErrorCommand { get; }

    public SelfImprovementViewModel(IKernelClient kernelClient)
    {
        _kernelClient = kernelClient;
        LoadStatusCommand = new AsyncRelayCommand(async _ => await LoadStatusAsync().ConfigureAwait(false));
        ClearErrorCommand = new RelayCommand(_ => ErrorMessage = "");
    }

    public SelfImprovementViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadStatusAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            Status = await _kernelClient.GetSelfImprovementStatusAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar status: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ClearError() => ErrorMessage = "";
}
