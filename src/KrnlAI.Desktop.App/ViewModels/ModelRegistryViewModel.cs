using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public class ModelRegistryViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private readonly ILogger<ModelRegistryViewModel> _logger;
    public ObservableCollection<ModelRegistryEntry> Models { get; } = [];
    private ModelRegistryEntry? _active;
    public ModelRegistryEntry? Active { get => _active; set => SetProperty(ref _active, value); }
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasNoData => !IsLoading && Models.Count == 0 && !HasError;
    private string _modelId = "anomaly-detector-v3";
    public string ModelId { get => _modelId; set => SetProperty(ref _modelId, value); }
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public ICommand LoadCommand { get; }

    public ModelRegistryViewModel(IKernelClient kernelClient, ILogger<ModelRegistryViewModel>? logger = null)
    {
        _kernelClient = kernelClient;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ModelRegistryViewModel>.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public ModelRegistryViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local)
            {
                ErrorMessage = "Indisponível no modo Local";
                return;
            }
            var detail = await _kernelClient.GetModelRegistryAsync(ModelId).ConfigureAwait(false);
            if (detail?.Models != null)
            {
                Models.Clear();
                foreach (var m in detail.Models) Models.Add(m);
                Active = detail.Active;
            }
            OnPropertyChanged(nameof(HasNoData));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar modelos: {ex.Message}";
            _logger.LogWarning(ex, "ModelRegistryViewModel.LoadAsync failed");
        }
        finally { IsLoading = false; }
    }
}
