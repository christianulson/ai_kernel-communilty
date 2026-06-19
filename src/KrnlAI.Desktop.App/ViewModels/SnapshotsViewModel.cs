using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

/// <summary>View model for the snapshots list page, displaying automatic system snapshots.</summary>
public class SnapshotsViewModel : ViewModelBase
{
    private readonly IKernelClient _client;
    private readonly ILogger<SnapshotsViewModel> _logger;
    private string _statusMessage = "";

    public SnapshotsViewModel() : this(ServiceLocator.Instance.KernelClient, ServiceLocator.Instance.GetLogger<SnapshotsViewModel>()) { }
    public SnapshotsViewModel(IKernelClient client, ILogger<SnapshotsViewModel>? logger = null)
    {
        _client = client;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SnapshotsViewModel>.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        CreateSnapshotCommand = new AsyncRelayCommand(CreateSnapshotAsync);
    }
    public ObservableCollection<SnapshotInfo> Snapshots { get; } = [];
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasNoData => !IsLoading && Snapshots.Count == 0 && !HasError;
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public ICommand LoadCommand { get; }
    public ICommand CreateSnapshotCommand { get; }

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

    private async Task CreateSnapshotAsync()
    {
        ErrorMessage = "";
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local) { StatusMessage = "Snapshot não disponível no modo Local"; return; }
            var result = await _client.SubmitFeedbackAsync(new FeedbackRequest("manual-snapshot", 5, "Snapshot via Desktop App", "snapshot"));
            StatusMessage = result != null ? "Snapshot criado com sucesso." : "Falha ao criar snapshot.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro: {ex.Message}";
            _logger.LogWarning(ex, "SnapshotsViewModel.CreateSnapshotAsync failed");
        }
    }
}
