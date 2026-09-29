using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class ExperimentsViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private string _newName = "", _newDescription = "", _errorMessage = "";
    private bool _isLoading;
    private ExperimentAnalysis? _currentAnalysis;

    public ObservableCollection<ExperimentInfo> Experiments { get; } = [];

    public string NewExperimentName { get => _newName; set => SetProperty(ref _newName, value); }
    public string NewExperimentDescription { get => _newDescription; set => SetProperty(ref _newDescription, value); }
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public ExperimentAnalysis? CurrentAnalysis { get => _currentAnalysis; set => SetProperty(ref _currentAnalysis, value); }

    public ICommand LoadExperimentsCommand { get; }
    public ICommand StartExperimentCommand { get; }
    public ICommand CompleteExperimentCommand { get; }
    public ICommand RecordMetricCommand { get; }
    public ICommand ViewAnalysisCommand { get; }
    public ICommand ClearErrorCommand { get; }
    public ICommand ClearAnalysisCommand { get; }

    public ExperimentsViewModel(IKernelClient kernelClient)
    {
        _kernelClient = kernelClient;
        LoadExperimentsCommand = new AsyncRelayCommand(LoadExperimentsAsync);
        StartExperimentCommand = new AsyncRelayCommand(StartExperimentAsync);
        CompleteExperimentCommand = new AsyncRelayCommand(p => CompleteExperimentAsync(p as string ?? ""));
        RecordMetricCommand = new AsyncRelayCommand(p => RecordMetricAsync(p as string ?? ""));
        ViewAnalysisCommand = new AsyncRelayCommand(p => ViewAnalysisAsync(p as string ?? ""));
        ClearErrorCommand = new RelayCommand(() => ErrorMessage = "");
        ClearAnalysisCommand = new RelayCommand(() => CurrentAnalysis = null);
    }

    public ExperimentsViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadExperimentsAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var items = await _kernelClient.ExperimentListAsync();
            Experiments.Clear();
            foreach (var e in items) Experiments.Add(e);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar experimentos: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task StartExperimentAsync()
    {
        if (string.IsNullOrWhiteSpace(NewExperimentName)) return;
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var request = new StartExperimentRequest(NewExperimentName.Trim(),
                string.IsNullOrWhiteSpace(NewExperimentDescription) ? null : NewExperimentDescription.Trim());
            await _kernelClient.ExperimentStartAsync(request);
            NewExperimentName = "";
            NewExperimentDescription = "";
            await LoadExperimentsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao iniciar experimento: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task CompleteExperimentAsync(string experimentId)
    {
        if (string.IsNullOrWhiteSpace(experimentId)) return;
        ErrorMessage = "";
        try
        {
            await _kernelClient.ExperimentCompleteAsync(experimentId);
            await LoadExperimentsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao concluir experimento: {ex.Message}";
        }
    }

    public async Task RecordMetricAsync(string experimentId, string metricName, double value)
    {
        if (string.IsNullOrWhiteSpace(experimentId) || string.IsNullOrWhiteSpace(metricName)) return;
        ErrorMessage = "";
        try
        {
            var request = new RecordMetricRequest(metricName, value);
            await _kernelClient.ExperimentRecordMetricAsync(experimentId, request);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao registrar métrica: {ex.Message}";
        }
    }

    public async Task ViewAnalysisAsync(string experimentId)
    {
        if (string.IsNullOrWhiteSpace(experimentId)) return;
        ErrorMessage = "";
        CurrentAnalysis = null;
        try
        {
            CurrentAnalysis = await _kernelClient.ExperimentGetAnalysisAsync(experimentId);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar análise: {ex.Message}";
        }
    }

    private async Task RecordMetricAsync(string? param)
    {
        if (string.IsNullOrWhiteSpace(param)) return;
        var parts = param.Split('|');
        if (parts.Length < 3) return;
        if (double.TryParse(parts[2], out var value))
            await RecordMetricAsync(parts[0], parts[1], value);
    }

    public void ClearError() => ErrorMessage = "";
    public void ClearAnalysis() => CurrentAnalysis = null;
}
