using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class ArchiveViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private ArchiveStats? _stats;
    public ArchiveStats? Stats { get => _stats; set => SetProperty(ref _stats, value); }
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasNoData => !IsLoading && Stats == null;
    public ICommand LoadCommand { get; }

    public ArchiveViewModel(IKernelClient kernelClient)
    {
        _kernelClient = kernelClient;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public ArchiveViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local) return;
            Stats = await _kernelClient.GetArchiveStatsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ArchiveViewModel.LoadAsync: {ex.Message}");
        }
        finally { IsLoading = false; }
    }
}
