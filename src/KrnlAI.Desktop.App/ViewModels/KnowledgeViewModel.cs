using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class KnowledgeViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private string _query = "", _errorMessage = "";
    private bool _isLoading, _isLearning;
    private KnowledgeStats? _stats;

    public ObservableCollection<KnowledgeHit> Results { get; } = [];

    public string Query { get => _query; set => SetProperty(ref _query, value); }
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public bool IsLearning { get => _isLearning; set => SetProperty(ref _isLearning, value); }
    public KnowledgeStats? Stats { get => _stats; set => SetProperty(ref _stats, value); }

    public ICommand SearchCommand { get; }
    public ICommand LoadStatsCommand { get; }
    public ICommand ClearErrorCommand { get; }
    public ICommand LearnCommand { get; }

    public KnowledgeViewModel(IKernelClient kernelClient)
    {
        _kernelClient = kernelClient;
        SearchCommand = new AsyncRelayCommand(SearchAsync);
        LoadStatsCommand = new AsyncRelayCommand(LoadStatsAsync);
        ClearErrorCommand = new RelayCommand(() => ErrorMessage = "");
        LearnCommand = new AsyncRelayCommand(async (param) =>
        {
            var p = param as string;
            if (string.IsNullOrWhiteSpace(p)) return;
            var parts = p.Split('|');
            var content = parts.Length > 0 ? parts[0].Trim() : "";
            var source = parts.Length > 1 ? parts[1].Trim() : "manual";
            var (_, error) = await LearnAsync(content, source);
            if (error != null) ErrorMessage = error;
        });
    }

    public KnowledgeViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(Query)) return;
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var r = await _kernelClient.KnowledgeAskAsync(Query);
            Results.Clear();
            if (r?.Hits != null)
                foreach (var h in r.Hits)
                    Results.Add(h);
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

    public async Task LoadStatsAsync()
    {
        ErrorMessage = "";
        try
        {
            Stats = await _kernelClient.KnowledgeStatsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar estatísticas: {ex.Message}";
        }
    }

    public async Task<(bool Success, string? Error)> LearnAsync(string content, string source, string? category = null)
    {
        IsLearning = true;
        try
        {
            var r = await _kernelClient.KnowledgeLearnAsync(content, source, category);
            if (r?.Success == true) return (true, null);
            return (false, r?.Error ?? "Falha ao aprender");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
        finally
        {
            IsLearning = false;
        }
    }

    public void ClearError() => ErrorMessage = "";
}
