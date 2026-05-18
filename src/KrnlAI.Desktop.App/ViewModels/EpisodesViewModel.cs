using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class EpisodesViewModel : ViewModelBase
{
    private readonly ServiceLocator _services;
    public ObservableCollection<EpisodeInfo> EpisodeList { get; } = new();
    private EpisodeDetails? _detail;
    public EpisodeDetails? EpisodeDetail { get => _detail; set => SetProperty(ref _detail, value); }
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasNoData => !IsLoading && EpisodeList.Count == 0 && !HasError;
    public ICommand LoadEpisodesCommand { get; }
    public ICommand ClearDetailCommand { get; }

    public EpisodesViewModel()
    {
        _services = ServiceLocator.Instance;
        LoadEpisodesCommand = new AsyncRelayCommand(LoadAsync);
        ClearDetailCommand = new RelayCommand(() => EpisodeDetail = null);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var r = await _services.KernelClient.SearchEpisodesAsync(new EpisodeSearchRequest(Page: 1, PageSize: 50));
            EpisodeList.Clear();
            foreach (var e in r.Episodes) EpisodeList.Add(e);
            OnPropertyChanged(nameof(HasNoData));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar episódios: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadDetailAsync(string id)
    {
        try
        {
            EpisodeDetail = await _services.KernelClient.GetEpisodeAsync(id);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar detalhe: {ex.Message}";
        }
    }
}
