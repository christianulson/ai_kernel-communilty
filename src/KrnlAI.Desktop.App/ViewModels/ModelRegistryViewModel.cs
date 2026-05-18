using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class ModelRegistryViewModel : ViewModelBase
{
    private readonly ServiceLocator _services;
    public ObservableCollection<ModelRegistryEntry> Models { get; } = new();
    private ModelRegistryEntry? _active;
    public ModelRegistryEntry? Active { get => _active; set => SetProperty(ref _active, value); }
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasNoData => !IsLoading && Models.Count == 0;
    private string _modelId = "anomaly-detector-v3";
    public string ModelId { get => _modelId; set => SetProperty(ref _modelId, value); }
    public ICommand LoadCommand { get; }

    public ModelRegistryViewModel()
    {
        _services = ServiceLocator.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var detail = await _services.KernelClient.GetModelRegistryAsync(ModelId);
            if (detail != null)
            {
                Models.Clear();
                foreach (var m in detail.Models) Models.Add(m);
                Active = detail.Active;
            }
        }
        finally { IsLoading = false; }
    }
}
