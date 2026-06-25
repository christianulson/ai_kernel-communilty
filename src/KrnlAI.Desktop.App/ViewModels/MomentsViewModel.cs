using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public class MomentsViewModel : ViewModelBase
{
    private readonly IKernelClient _client;
    private readonly ILogger<MomentsViewModel> _logger;
    public ObservableCollection<MemoryMoment> Moments { get; } = [];
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasNoData => !IsLoading && Moments.Count == 0 && !HasError;
    public ICommand LoadCommand { get; }

    public MomentsViewModel() : this(ServiceLocator.Instance.KernelClient, ServiceLocator.Instance.GetLogger<MomentsViewModel>()) { }
    public MomentsViewModel(IKernelClient client, ILogger<MomentsViewModel>? logger = null)
    {
        _client = client; _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<MomentsViewModel>.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public async Task LoadAsync()
    {
        IsLoading = true; ErrorMessage = "";
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local) { ErrorMessage = "Indisponível no modo Local"; return; }
            var r = await _client.GetMemoryMomentsAsync(50).ConfigureAwait(false);
            Moments.Clear();
            if (r != null) foreach (var m in r) Moments.Add(m);
            OnPropertyChanged(nameof(HasNoData));
        }
        catch (Exception ex) { ErrorMessage = $"Erro: {ex.Message}"; _logger.LogWarning(ex, "MomentsViewModel.LoadAsync failed"); }
        finally { IsLoading = false; }
    }
}
