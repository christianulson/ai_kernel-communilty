using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class MemoryViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    public ObservableCollection<MemoryHit> MemoryResults { get; } = [];
    public ObservableCollection<WorkingMemorySlot> WorkingSlots { get; } = [];
    private MemoryMetrics? _metrics;
    public MemoryMetrics? MemoryMetricsData { get => _metrics; set => SetProperty(ref _metrics, value); }
    private string _query = "", _tab = "search", _errorMessage = "";
    public string MemoryQuery { get => _query; set => SetProperty(ref _query, value); }
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public string MemoryTab { get => _tab; set { if (SetProperty(ref _tab, value)) { OnPropertyChanged(nameof(IsSearch)); OnPropertyChanged(nameof(IsMetrics)); OnPropertyChanged(nameof(IsWorking)); } } }
    public bool IsSearch => _tab == "search";
    public bool IsMetrics => _tab == "metrics";
    public bool IsWorking => _tab == "working";
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

    public ICommand SearchMemoryCommand { get; }
    public ICommand LoadMetricsCommand { get; }
    public ICommand LoadWorkingCommand { get; }
    public ICommand SetTabSearchCommand { get; }
    public ICommand SetTabMetricsCommand { get; }
    public ICommand SetTabWorkingCommand { get; }
    public ICommand ClearErrorCommand { get; }

    public MemoryViewModel(IKernelClient kernelClient)
    {
        _kernelClient = kernelClient;
        SearchMemoryCommand = new AsyncRelayCommand(SearchAsync);
        LoadMetricsCommand = new AsyncRelayCommand(async () =>
        {
            try { MemoryMetricsData = await _kernelClient.GetMemoryMetricsAsync(); }
            catch (Exception ex) { ErrorMessage = $"Erro ao carregar métricas: {ex.Message}"; }
        });
        LoadWorkingCommand = new AsyncRelayCommand(async () =>
        {
            try
            {
                var s = await _kernelClient.GetWorkingMemoryAsync();
                WorkingSlots.Clear();
                if (s?.Slots != null) foreach (var x in s.Slots) WorkingSlots.Add(x);
            }
            catch (Exception ex) { ErrorMessage = $"Erro ao carregar working memory: {ex.Message}"; }
        });
        SetTabSearchCommand = new RelayCommand(() => MemoryTab = "search");
        SetTabMetricsCommand = new RelayCommand(() => MemoryTab = "metrics");
        SetTabWorkingCommand = new RelayCommand(() => MemoryTab = "working");
        ClearErrorCommand = new RelayCommand(() => ErrorMessage = "");
    }

    public MemoryViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(MemoryQuery)) return;
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var r = await _kernelClient.SearchMemoryAsync(MemoryQuery, 20);
            MemoryResults.Clear();
            if (r?.Hits != null) foreach (var h in r.Hits) MemoryResults.Add(h);
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
