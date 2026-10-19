using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class EpisodicMemoryViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private string _searchQuery = "";
    private string _errorMessage = "";
    private bool _isLoading;
    private EpisodicMemoryHit? _selectedEpisode;

    public ObservableCollection<EpisodicMemoryHit> Results { get; } = [];

    public string SearchQuery { get => _searchQuery; set => SetProperty(ref _searchQuery, value); }
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public EpisodicMemoryHit? SelectedEpisode { get => _selectedEpisode; set => SetProperty(ref _selectedEpisode, value); }

    public ICommand SearchCommand { get; }
    public ICommand SelectEpisodeCommand { get; }
    public ICommand ClearErrorCommand { get; }

    public EpisodicMemoryViewModel(IKernelClient kernelClient)
    {
        _kernelClient = kernelClient;
        SearchCommand = new AsyncRelayCommand(SearchAsync);
        SelectEpisodeCommand = new RelayCommand(param =>
        {
            if (param is EpisodicMemoryHit hit) SelectedEpisode = hit;
        });
        ClearErrorCommand = new RelayCommand(() => ErrorMessage = "");
    }

    public EpisodicMemoryViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var request = new EpisodicMemorySearchRequest(SearchQuery.Trim(), TopK: 20);
            var result = await _kernelClient.SearchEpisodicMemoryAsync(request);
            Results.Clear();
            if (result?.Hits != null)
                foreach (var hit in result.Hits)
                    Results.Add(hit);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro na busca: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
