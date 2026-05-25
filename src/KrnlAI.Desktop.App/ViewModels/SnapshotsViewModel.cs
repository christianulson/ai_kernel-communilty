using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class SnapshotsViewModel : ViewModelBase
{
    private readonly IKernelClient _client;

    public SnapshotsViewModel() : this(ServiceLocator.Instance.KernelClient) { }
    public SnapshotsViewModel(IKernelClient client) { _client = client; LoadCommand = new AsyncRelayCommand(LoadAsync); }
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
            foreach (var s in r) Snapshots.Add(s);
            OnPropertyChanged(nameof(HasNoData));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar snapshots: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
