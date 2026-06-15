using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public class SchedulerViewModel : ViewModelBase
{
    private readonly IKernelClient _client;
    private readonly ILogger<SchedulerViewModel> _logger;
    public ObservableCollection<ScheduledTask> Tasks { get; } = new();
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasNoData => !IsLoading && Tasks.Count == 0 && !HasError;
    public ICommand LoadCommand { get; }

    public SchedulerViewModel() : this(ServiceLocator.Instance.KernelClient, ServiceLocator.Instance.GetLogger<SchedulerViewModel>()) { }
    public SchedulerViewModel(IKernelClient client, ILogger<SchedulerViewModel>? logger = null)
    {
        _client = client; _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SchedulerViewModel>.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public async Task LoadAsync()
    {
        IsLoading = true; ErrorMessage = "";
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local) { ErrorMessage = "Indisponível no modo Local"; return; }
            var r = await _client.GetScheduledTasksAsync();
            Tasks.Clear();
            if (r != null) foreach (var t in r) Tasks.Add(t);
            OnPropertyChanged(nameof(HasNoData));
        }
        catch (Exception ex) { ErrorMessage = $"Erro: {ex.Message}"; _logger.LogWarning(ex, "SchedulerViewModel.LoadAsync failed"); }
        finally { IsLoading = false; }
    }
}
