using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class VersionsViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    public ObservableCollection<ContractEntry> Contracts { get; } = new();
    private VersionsInfo? _versions;
    public VersionsInfo? Versions { get => _versions; set => SetProperty(ref _versions, value); }
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasNoData => !IsLoading && Versions == null;
    public ICommand LoadCommand { get; }

    public VersionsViewModel(IKernelClient kernelClient)
    {
        _kernelClient = kernelClient;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public VersionsViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local) return;
            Versions = await _kernelClient.GetVersionsAsync();
            var contractsResp = await _kernelClient.GetContractsAsync();
            Contracts.Clear();
            if (contractsResp?.Contracts != null)
                foreach (var c in contractsResp.Contracts) Contracts.Add(c);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"VersionsViewModel.LoadAsync: {ex.Message}");
        }
        finally { IsLoading = false; }
    }
}
