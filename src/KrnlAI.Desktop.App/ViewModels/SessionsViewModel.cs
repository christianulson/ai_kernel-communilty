using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class SessionsViewModel : ViewModelBase
{
    private readonly ServiceLocator _services;
    public ObservableCollection<SessionShare> Shares { get; } = new();
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasNoData => !IsLoading && Shares.Count == 0;
    public ICommand LoadCommand { get; }

    public SessionsViewModel()
    {
        _services = ServiceLocator.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var resp = await _services.KernelClient.GetSharesAsync();
            Shares.Clear();
            if (resp != null)
                foreach (var s in resp.Shares) Shares.Add(s);
        }
        finally { IsLoading = false; }
    }
}
