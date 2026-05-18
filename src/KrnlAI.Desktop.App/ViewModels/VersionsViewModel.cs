using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class VersionsViewModel : ViewModelBase
{
    private readonly ServiceLocator _services;
    public ObservableCollection<ContractEntry> Contracts { get; } = new();
    private VersionsInfo? _versions;
    public VersionsInfo? Versions { get => _versions; set => SetProperty(ref _versions, value); }
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasNoData => !IsLoading && Versions == null;
    public ICommand LoadCommand { get; }

    public VersionsViewModel()
    {
        _services = ServiceLocator.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Versions = await _services.KernelClient.GetVersionsAsync();
            var contractsResp = await _services.KernelClient.GetContractsAsync();
            Contracts.Clear();
            if (contractsResp != null)
                foreach (var c in contractsResp.Contracts) Contracts.Add(c);
        }
        finally { IsLoading = false; }
    }
}
