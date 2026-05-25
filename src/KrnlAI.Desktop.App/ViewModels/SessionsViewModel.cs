using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class SessionsViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    public ObservableCollection<SessionShare> Shares { get; } = new();
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasNoData => !IsLoading && Shares.Count == 0;
    public ICommand LoadCommand { get; }

    public SessionsViewModel(IKernelClient kernelClient)
    {
        _kernelClient = kernelClient;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public SessionsViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local) return;
            var resp = await _kernelClient.GetSharesAsync();
            Shares.Clear();
            if (resp?.Shares != null)
                foreach (var s in resp.Shares) Shares.Add(s);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SessionsViewModel.LoadAsync: {ex.Message}");
        }
        finally { IsLoading = false; }
    }
}
