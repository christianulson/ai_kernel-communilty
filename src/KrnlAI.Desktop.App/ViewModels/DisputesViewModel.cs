using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public sealed class DisputesViewModel : ViewModelBase
{
    private readonly ILogger<DisputesViewModel> _logger;
    private object? _selectedDispute;

    public DisputesViewModel(ILogger<DisputesViewModel>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DisputesViewModel>.Instance;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        ResolveForWorkerCommand = new RelayCommand(() => { });
        ResolveForSolicitorCommand = new RelayCommand(() => { });
    }

    public ObservableCollection<object> Disputes { get; } = [];

    public object? SelectedDispute
    {
        get => _selectedDispute;
        set
        {
            SetProperty(ref _selectedDispute, value);
            OnPropertyChanged(nameof(HasSelectedDispute));
        }
    }

    public bool HasSelectedDispute => _selectedDispute != null;

    public ICommand RefreshCommand { get; }
    public ICommand ResolveForWorkerCommand { get; }
    public ICommand ResolveForSolicitorCommand { get; }

    private async Task RefreshAsync()
    {
        try
        {
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh disputes");
        }
    }
}
