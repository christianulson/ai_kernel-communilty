using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public class SnapshotsViewModel : ViewModelBase
{
    private readonly IKernelClient _client;
    private readonly ILogger<SnapshotsViewModel> _logger;

    public SnapshotsViewModel() : this(ServiceLocator.Instance.KernelClient, ServiceLocator.Instance.GetLogger<SnapshotsViewModel>()) { }
    public SnapshotsViewModel(IKernelClient client, ILogger<SnapshotsViewModel>? logger = null)
    {
        _client = client;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SnapshotsViewModel>.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }
    public ObservableCollection<SnapshotInfo> Snapshots { get; } = new();
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasNoData => !IsLoading && Snapshots.Count == 0 && !HasError;
    public ICommand LoadCommand { get; }

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
            var r = await _client.GetSnapshotsAsync();
            Snapshots.Clear();
            if (r != null) { foreach (var s in r) Snapshots.Add(s); }
            OnPropertyChanged(nameof(HasNoData));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar snapshots: {ex.Message}";
            _logger.LogWarning(ex, "SnapshotsViewModel.LoadAsync failed");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
