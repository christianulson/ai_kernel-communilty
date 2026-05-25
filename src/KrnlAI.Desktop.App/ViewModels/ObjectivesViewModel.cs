using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class ObjectivesViewModel : ViewModelBase
{
    private readonly IKernelClient _client;
    public ObservableCollection<ObjectiveInfo> Objectives { get; } = new();
    private ObjectiveDetail? _selectedObjective;
    public ObjectiveDetail? SelectedObjective { get => _selectedObjective; set => SetProperty(ref _selectedObjective, value); }
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasNoData => !IsLoading && Objectives.Count == 0 && !HasError;
    public ICommand LoadCommand { get; }
    public ICommand ClearDetailCommand { get; }

    public ObjectivesViewModel() : this(ServiceLocator.Instance.KernelClient) { }
    public ObjectivesViewModel(IKernelClient client) { _client = client; LoadCommand = new AsyncRelayCommand(LoadAsync); ClearDetailCommand = new RelayCommand(() => SelectedObjective = null); }

    public async Task LoadAsync()
    {
        IsLoading = true; ErrorMessage = "";
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local)
            {
                ErrorMessage = "Indisponível no modo Local";
                return;
            }
            var r = await _client.GetObjectivesAsync(); Objectives.Clear(); foreach (var o in r) Objectives.Add(o); OnPropertyChanged(nameof(HasNoData));
        }
        catch (Exception ex) { ErrorMessage = $"Erro ao carregar objetivos: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    public async Task LoadDetailAsync(string id)
    {
        if (ServiceLocator.Instance.CurrentMode == RunMode.Local) return;
        try { SelectedObjective = await _client.GetObjectiveDetailAsync(id); }
        catch (Exception ex) { ErrorMessage = $"Erro ao carregar detalhe: {ex.Message}"; }
    }
}
