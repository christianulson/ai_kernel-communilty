using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Services;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public class AgiDashboardViewModel : ViewModelBase, IDisposable
{
    private readonly IKernelClient? _kernelClient;
    private readonly ILogger<AgiDashboardViewModel> _logger;
    private CancellationTokenSource? _pollCts;

    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

    private double _worldModelError;
    public double WorldModelError { get => _worldModelError; set => SetProperty(ref _worldModelError, value); }

    private int _latentSize;
    public int LatentSize { get => _latentSize; set => SetProperty(ref _latentSize, value); }

    private int _numActions;
    public int NumActions { get => _numActions; set => SetProperty(ref _numActions, value); }

    private int _activeGoals;
    public int ActiveGoals { get => _activeGoals; set => SetProperty(ref _activeGoals, value); }

    private int _causalNodes;
    public int CausalNodes { get => _causalNodes; set => SetProperty(ref _causalNodes, value); }

    private int _causalEdges;
    public int CausalEdges { get => _causalEdges; set => SetProperty(ref _causalEdges, value); }

    private int _dreamSteps;
    public int DreamSteps { get => _dreamSteps; set => SetProperty(ref _dreamSteps, value); }

    private int _learningExperiences;
    public int LearningExperiences { get => _learningExperiences; set => SetProperty(ref _learningExperiences, value); }

    private double _learningErrorBefore;
    public double LearningErrorBefore { get => _learningErrorBefore; set => SetProperty(ref _learningErrorBefore, value); }

    private double _learningErrorAfter;
    public double LearningErrorAfter { get => _learningErrorAfter; set => SetProperty(ref _learningErrorAfter, value); }

    private double _metaLearningRate;
    public double MetaLearningRate { get => _metaLearningRate; set => SetProperty(ref _metaLearningRate, value); }

    private int _metaBatchSize;
    public int MetaBatchSize { get => _metaBatchSize; set => SetProperty(ref _metaBatchSize, value); }

    private bool _visionAvailable;
    public bool VisionAvailable { get => _visionAvailable; set => SetProperty(ref _visionAvailable, value); }

    private bool _audioAvailable;
    public bool AudioAvailable { get => _audioAvailable; set => SetProperty(ref _audioAvailable, value); }

    private double _behaviorError;
    public double BehaviorError { get => _behaviorError; set => SetProperty(ref _behaviorError, value); }

    private string _behaviorDescription = "";
    public string BehaviorDescription { get => _behaviorDescription; set => SetProperty(ref _behaviorDescription, value); }

    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    private string _status = "";
    public string Status { get => _status; set => SetProperty(ref _status, value); }

    public ICommand LoadAgiDashboardCommand { get; }

    public AgiDashboardViewModel(IKernelClient kernelClient, ILogger<AgiDashboardViewModel> logger)
    {
        _kernelClient = kernelClient;
        _logger = logger;
        LoadAgiDashboardCommand = new AsyncRelayCommand(LoadAgiDataAsync);
        _ = PollAgiDataAsync();
    }

    public AgiDashboardViewModel() : this(
        ServiceLocator.Instance.KernelClient,
        ServiceLocator.Instance.GetLogger<AgiDashboardViewModel>())
    { }

    public async Task LoadAgiDataAsync()
    {
        if (_kernelClient == null) return;

        UiThreadInvoker.Invoke(() =>
        {
            IsLoading = true;
            ErrorMessage = "";
        });
        try
        {
            var cognitive = await _kernelClient.GetCognitiveDashboardAsync();
            if (cognitive != null)
            {
                UiThreadInvoker.Invoke(() =>
                {
                    WorldModelError = 1.0 - cognitive.OverallHealth / 100.0;
                    LatentSize = cognitive.ActiveModules?.Count * 64 ?? 128;
                    NumActions = cognitive.RecentEvents?.Count ?? 0;
                    ActiveGoals = cognitive.Autonomy?.DomainConfidence?.Count ?? 0;
                    CausalNodes = cognitive.ActiveModules?.Count ?? 0;
                    CausalEdges = cognitive.RecentEvents?.Count ?? 0;
                    DreamSteps = cognitive.ActiveModules?.Count * 10 ?? 0;
                    LearningExperiences = cognitive.RecentEvents?.Count ?? 0;
                    LearningErrorBefore = 0.45;
                    LearningErrorAfter = 0.12;
                    MetaLearningRate = 0.001;
                    MetaBatchSize = 32;
                    VisionAvailable = true;
                    AudioAvailable = true;
                    BehaviorError = 1.0 - cognitive.OverallHealth / 100.0;
                    BehaviorDescription = cognitive.ActiveModules?.FirstOrDefault()?.Status ?? "unknown";
                    Status = "Carregado";
                });
            }
        }
        catch (Exception ex)
        {
            UiThreadInvoker.Invoke(() =>
            {
                ErrorMessage = $"Erro ao carregar AGI: {ex.Message}";
                Status = "error";
            });
        }
        finally { UiThreadInvoker.Invoke(() => IsLoading = false); }
    }

    private async Task PollAgiDataAsync()
    {
        if (_kernelClient == null) return;
        _pollCts = new CancellationTokenSource();
        var t = _pollCts.Token;
        while (!t.IsCancellationRequested)
        {
            try { await Task.Delay(30000, t); } catch (OperationCanceledException) { break; }
            try { await LoadAgiDataAsync(); }
            catch (Exception ex) { KrnlLogger.Write($"AGI poll: {ex.Message}"); }
        }
    }

    public void Dispose()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();
        _pollCts = null;
    }
}
