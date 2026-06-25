using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class PlanViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private PlanInfo? _currentPlan;
    private string _errorMessage = "";
    private bool _isLoading;

    public ObservableCollection<PlanStep> Steps { get; } = [];

    public PlanInfo? CurrentPlan { get => _currentPlan; set { SetProperty(ref _currentPlan, value); OnPropertyChanged(nameof(Progress)); } }
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public double Progress => CurrentPlan?.Progress ?? 0.0;

    public ICommand LoadPlanCommand { get; }
    public ICommand ClearErrorCommand { get; }

    public PlanViewModel(IKernelClient kernelClient)
    {
        _kernelClient = kernelClient;
        LoadPlanCommand = new AsyncRelayCommand(LoadPlanAsync);
        ClearErrorCommand = new RelayCommand(() => ErrorMessage = "");
    }

    public PlanViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadPlanAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var result = await _kernelClient.GetCurrentPlanAsync().ConfigureAwait(false);
            if (result != null)
            {
                CurrentPlan = result.CurrentPlan;
                Steps.Clear();
                foreach (var step in result.Steps)
                    Steps.Add(step);
            }
            else
            {
                CurrentPlan = null;
                Steps.Clear();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar plano: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public Task RefreshAsync() => LoadPlanAsync();
}
