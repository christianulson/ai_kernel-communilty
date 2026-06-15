using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public class DashboardViewModel : ViewModelBase, IDisposable
{
    private readonly IKernelClient? _kernelClient;
    private readonly ILogger<DashboardViewModel> _logger;
    private CancellationTokenSource? _emotionalPollCts;
    public ObservableCollection<GoalInfo> GoalsList { get; } = new();
    private AgentScorecard? _scorecard;
    public AgentScorecard? ScorecardData { get => _scorecard; set => SetProperty(ref _scorecard, value); }
    private RuntimeSummary? _runtime;
    public RuntimeSummary? RuntimeData { get => _runtime; set => SetProperty(ref _runtime, value); }
    private AgentMetricsSummary? _metrics;
    public AgentMetricsSummary? MetricsData { get => _metrics; set => SetProperty(ref _metrics, value); }
    private CognitiveDashboardData? _cognitive;
    public CognitiveDashboardData? CognitiveData { get => _cognitive; set => SetProperty(ref _cognitive, value); }
    private CrossSummaryData? _cross;
    public CrossSummaryData? CrossSummaryData { get => _cross; set => SetProperty(ref _cross, value); }
    private GoalInfo? _selectedGoal;
    public GoalInfo? SelectedGoal { get => _selectedGoal; set { SetProperty(ref _selectedGoal, value); if (value != null) _ = LoadGoalDetailSafeAsync(value.GoalId); } }
    private GoalDetails? _goalDetail;
    public GoalDetails? GoalDetail { get => _goalDetail; set => SetProperty(ref _goalDetail, value); }
    private string _newDesc = "";
    public string NewGoalDescription { get => _newDesc; set => SetProperty(ref _newDesc, value); }
    private int _newPri = 3;
    public int NewGoalPriority { get => _newPri; set => SetProperty(ref _newPri, value); }
    private bool _isCreateVisible;
    public bool IsGoalCreateVisible { get => _isCreateVisible; set => SetProperty(ref _isCreateVisible, value); }
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    private string _status = "";
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    private GoalCycleList? _goalCycles;
    public GoalCycleList? GoalCycles { get => _goalCycles; set => SetProperty(ref _goalCycles, value); }
    private MetricsByGoalData? _metricsByGoal;
    public MetricsByGoalData? MetricsByGoalData { get => _metricsByGoal; set => SetProperty(ref _metricsByGoal, value); }
    private BenchmarkSummary? _benchmark;
    public BenchmarkSummary? BenchmarkData { get => _benchmark; set => SetProperty(ref _benchmark, value); }
    private EmotionalState? _emotional;
    public EmotionalState? EmotionalState { get => _emotional; set { SetProperty(ref _emotional, value); OnPropertyChanged(nameof(EmotionalMood)); OnPropertyChanged(nameof(EmotionalTone)); OnPropertyChanged(nameof(EmotionalMotive)); } }
    private AffectiveState? _affective;
    public AffectiveState? AffectiveState { get => _affective; set => SetProperty(ref _affective, value); }
    public string EmotionalMood
    {
        get
        {
            if (_emotional == null) return "—";
            var (v, a) = (_emotional.Valence, _emotional.Arousal);
            if (v > 0.3) return a < 0.4 ? "😌 Tranquilo" : "⚡ Animado";
            if (v < -0.3) return a < 0.4 ? "😮‍💨 Cansado" : "😰 Tenso";
            return a >= 0.4 ? "🧐 Atento" : "😐 Neutro";
        }
    }
    public string EmotionalTone
    {
        get
        {
            if (_emotional == null) return "Neutral";
            var (v, a) = (_emotional.Valence, _emotional.Arousal);
            if (v > 0.3) return a < 0.4 ? "Success" : "Info";
            if (v < -0.3) return a < 0.4 ? "Warning" : "Danger";
            return "Neutral";
        }
    }
    public string EmotionalMotive
    {
        get
        {
            if (_emotional == null) return "—";
            var (v, a) = (_emotional.Valence, _emotional.Arousal);
            if (v < -0.5 && a > 0.7) return "Medo";
            if (v > 0.5 && a < 0.3) return "Satisfação";
            if (v > 0.3 && a >= 0.3 && a <= 0.6) return "Esperança";
            if (v > 0 && a > 0.5) return "Curiosidade";
            if (v < -0.3 && a >= 0.3 && a <= 0.6) return "Frustração";
            if (v < 0 && a < 0.3) return "Cautela";
            if (v < 0 && a > 0.6) return "Urgência";
            return "—";
        }
    }

    public ICommand LoadDashboardCommand { get; }
    public ICommand LoadGoalsCommand { get; }
    public ICommand CreateGoalCommand { get; }
    public ICommand PauseGoalCommand { get; }
    public ICommand ResumeGoalCommand { get; }
    public ICommand CompleteGoalCommand { get; }
    public ICommand LoadCognitiveCommand { get; }
    public ICommand LoadBenchmarkCommand { get; }
    public ICommand LoadCrossSummaryCommand { get; }
    public ICommand LoadMetricsByGoalCommand { get; }
    public ICommand ShowCreateGoalCommand { get; }
    public ICommand HideCreateGoalCommand { get; }

    public DashboardViewModel(IKernelClient kernelClient, ILogger<DashboardViewModel> logger)
    {
        _kernelClient = kernelClient;
        _logger = logger;
        LoadDashboardCommand = new AsyncRelayCommand(LoadDashboardDataAsync);
        LoadGoalsCommand = new AsyncRelayCommand(LoadGoalsDataAsync);
        CreateGoalCommand = new AsyncRelayCommand(CreateNewGoalAsync);
        PauseGoalCommand = new AsyncRelayCommand(() => UpdateGoalAsync("pause"));
        ResumeGoalCommand = new AsyncRelayCommand(() => UpdateGoalAsync("resume"));
        CompleteGoalCommand = new AsyncRelayCommand(() => UpdateGoalAsync("complete"));
        LoadCognitiveCommand = new AsyncRelayCommand(async () => { if (_kernelClient == null) return; CognitiveData = await _kernelClient.GetCognitiveDashboardAsync(); });
        LoadBenchmarkCommand = new AsyncRelayCommand(async () => { if (_kernelClient == null) return; BenchmarkData = await _kernelClient.GetBenchmarkSummaryAsync(); });
        LoadCrossSummaryCommand = new AsyncRelayCommand(async () => { if (_kernelClient == null) return; CrossSummaryData = await _kernelClient.GetCrossSummaryAsync(); });
        LoadMetricsByGoalCommand = new AsyncRelayCommand(async () => { if (_kernelClient == null) return; MetricsByGoalData = await _kernelClient.GetMetricsByGoalAsync(); });
        ShowCreateGoalCommand = new RelayCommand(() => IsGoalCreateVisible = true);
        HideCreateGoalCommand = new RelayCommand(() => { IsGoalCreateVisible = false; NewGoalDescription = ""; });
        _ = PollEmotionalStateAsync();
    }

    public DashboardViewModel() : this(
        ServiceLocator.Instance.KernelClient,
        ServiceLocator.Instance.GetLogger<DashboardViewModel>())
    { }

    private const int MaxPollFailures = 3;

    private async Task PollEmotionalStateAsync()
    {
        if (_kernelClient == null) return;
        _emotionalPollCts = new CancellationTokenSource();
        var t = _emotionalPollCts.Token;
        var consecutiveFailures = 0;
        while (!t.IsCancellationRequested)
        {
            try
            {
                var state = await _kernelClient.GetEmotionalStateAsync("admin-001", t);
                if (state != null)
                {
                    consecutiveFailures = 0;
                    UiThreadInvoker.Invoke(() => EmotionalState = state);
                }
                else if (++consecutiveFailures >= MaxPollFailures)
                {
                    break;
                }
                var affective = await _kernelClient.GetAffectiveStateAsync(t);
                if (affective != null)
                {
                    UiThreadInvoker.Invoke(() => AffectiveState = affective);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { KrnlAI.Desktop.Core.Services.KrnlLogger.Write($"Dash emotional poll: {ex.Message}"); }
            try { await Task.Delay(15000, t); } catch (OperationCanceledException) { break; }
        }
    }

    public void Dispose()
    {
        _emotionalPollCts?.Cancel();
        _emotionalPollCts?.Dispose();
        _emotionalPollCts = null;
    }

    public async Task LoadDashboardDataAsync()
    {
        if (_kernelClient == null) return;

        UiThreadInvoker.Invoke(() =>
        {
            IsLoading = true;
            ErrorMessage = "";
        });
        try
        {
            var scorecardTask = _kernelClient.GetScorecardAsync();
            var runtimeTask = _kernelClient.GetRuntimeSummaryAsync();
            var metricsTask = _kernelClient.GetMetricsSummaryAsync();
            var goalsTask = _kernelClient.GetActiveGoalsAsync();
            var cognitiveTask = _kernelClient.GetCognitiveDashboardAsync();
            var crossTask = _kernelClient.GetCrossSummaryAsync();
            var metricsByGoalTask = _kernelClient.GetMetricsByGoalAsync();
            var emotionalTask = _kernelClient.GetEmotionalStateAsync("admin-001");
            var affectiveTask = _kernelClient.GetAffectiveStateAsync();
            await Task.WhenAll(scorecardTask, runtimeTask, metricsTask, goalsTask, cognitiveTask, crossTask, metricsByGoalTask, emotionalTask, affectiveTask);
            var scorecard = await scorecardTask;
            var runtime = await runtimeTask;
            var metrics = await metricsTask;
            var goals = await goalsTask;
            var cognitive = await cognitiveTask;
            var cross = await crossTask;
            var metricsByGoal = await metricsByGoalTask;
            var emotional = await emotionalTask;
            var affective = await affectiveTask;

            UiThreadInvoker.Invoke(() =>
            {
                ScorecardData = scorecard;
                RuntimeData = runtime;
                MetricsData = metrics;
                GoalsList.Clear();
                if (goals?.Goals != null)
                    foreach (var g in goals.Goals) GoalsList.Add(g);
                CognitiveData = cognitive;
                CrossSummaryData = cross;
                MetricsByGoalData = metricsByGoal;
                EmotionalState = emotional;
                AffectiveState = affective;
                Status = "Carregado";
            });
        }
        catch (Exception ex)
        {
            UiThreadInvoker.Invoke(() =>
            {
                ErrorMessage = $"Erro ao carregar dashboard: {ex.Message}";
                Status = "Erro";
            });
        }
        finally { UiThreadInvoker.Invoke(() => IsLoading = false); }
    }

    private async Task LoadGoalsDataAsync()
    {
        if (_kernelClient == null) return;
        try
        {
            var r = await _kernelClient.GetActiveGoalsAsync();
            if (r?.Goals == null) return;
            UiThreadInvoker.Invoke(() =>
            {
                GoalsList.Clear();
                foreach (var g in r.Goals) GoalsList.Add(g);
            });
        }
        catch (Exception ex) { _logger.LogError(ex, "DashboardViewModel.LoadGoalsDataAsync"); }
    }

    private async Task CreateNewGoalAsync()
    {
        if (_kernelClient == null || string.IsNullOrWhiteSpace(NewGoalDescription)) return;
        await _kernelClient.CreateGoalAsync(new CreateGoalRequest(NewGoalDescription, NewGoalPriority));
        UiThreadInvoker.Invoke(() =>
        {
            NewGoalDescription = "";
            IsGoalCreateVisible = false;
        });
        await LoadGoalsDataAsync();
    }

    private async Task UpdateGoalAsync(string a)
    {
        if (_kernelClient == null || SelectedGoal == null) return;
        await _kernelClient.UpdateGoalStatusAsync(SelectedGoal.GoalId, a);
        await LoadGoalsDataAsync();
    }

    private async Task LoadGoalDetailSafeAsync(string id)
    {
        if (_kernelClient == null) return;
        try
        {
            var detail = await _kernelClient.GetGoalAsync(id);
            var cycles = await _kernelClient.GetGoalCyclesAsync(id);
            UiThreadInvoker.Invoke(() =>
            {
                GoalDetail = detail;
                GoalCycles = cycles;
            });
        }
        catch (Exception ex)
        {
            UiThreadInvoker.Invoke(() => ErrorMessage = $"Erro ao carregar detalhe do goal: {ex.Message}");
        }
    }

    private async Task LoadGoalDetailAsync(string id)
    {
        if (_kernelClient == null) return;
        var detail = await _kernelClient.GetGoalAsync(id);
        var cycles = await _kernelClient.GetGoalCyclesAsync(id);
        UiThreadInvoker.Invoke(() =>
        {
            GoalDetail = detail;
            GoalCycles = cycles;
        });
    }
}
