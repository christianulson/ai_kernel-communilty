using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class ArchiveViewModel : ViewModelBase
{
    private readonly ServiceLocator _services;
    private ArchiveStats? _stats;
    public ArchiveStats? Stats { get => _stats; set => SetProperty(ref _stats, value); }
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasNoData => !IsLoading && Stats == null;
    public ICommand LoadCommand { get; }

    public ArchiveViewModel()
    {
        _services = ServiceLocator.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try { Stats = await _services.KernelClient.GetArchiveStatsAsync(); }
        finally { IsLoading = false; }
    }
}
