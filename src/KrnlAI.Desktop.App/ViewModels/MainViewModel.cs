using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Desktop.Core.Services;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private readonly IListeningService _listeningService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<MainViewModel> _logger;
    private CancellationTokenSource? _healthCheckCts;
    private CancellationTokenSource? _emotionalPollCts;
    private EmotionalState? _emotionalState;
    public EmotionalState? EmotionalState { get => _emotionalState; set { SetProperty(ref _emotionalState, value); OnPropertyChanged(nameof(EmotionalMood)); OnPropertyChanged(nameof(EmotionalTone)); OnPropertyChanged(nameof(EmotionalMotive)); } }
    public string EmotionalMood
    {
        get
        {
            if (_emotionalState == null) return "—";
            var (v, a) = (_emotionalState.Valence, _emotionalState.Arousal);
            if (v > 0.3) return a < 0.4 ? "😌 Tranquilo" : "⚡ Animado";
            if (v < -0.3) return a < 0.4 ? "😮‍💨 Cansado" : "😰 Tenso";
            return a >= 0.4 ? "🧐 Atento" : "😐 Neutro";
        }
    }
    public string EmotionalTone
    {
        get
        {
            if (_emotionalState == null) return "Neutral";
            var (v, a) = (_emotionalState.Valence, _emotionalState.Arousal);
            if (v > 0.3) return a < 0.4 ? "Success" : "Info";
            if (v < -0.3) return a < 0.4 ? "Warning" : "Danger";
            return "Neutral";
        }
    }
    public string EmotionalMotive
    {
        get
        {
            if (_emotionalState == null) return "—";
            var (v, a) = (_emotionalState.Valence, _emotionalState.Arousal);
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
    public string UserId { get; set; } = "dev-user";

    public ChatViewModel ChatVM { get; }
    public DashboardViewModel DashVM { get; }
    public SettingsViewModel SettingsVM { get; }
    public MemoryViewModel MemoryVM { get; }
    public EpisodesViewModel EpisodesVM { get; }
    public DocumentViewModel DocumentVM { get; }
    public PoliciesViewModel PoliciesVM { get; }
    public BenchmarkViewModel BenchmarkVM { get; }
    public CausalGraphViewModel CausalVM { get; }
    public ProfileViewModel ProfileVM { get; }
    public ArchiveViewModel ArchiveVM { get; }
    public ModelRegistryViewModel ModelRegistryVM { get; }
    public VersionsViewModel VersionsVM { get; }
    public SessionsViewModel SessionsVM { get; }

    public ObservableCollection<MediaDevice> Microphones => SettingsVM.Microphones;
    public ObservableCollection<MediaDevice> Cameras => SettingsVM.Cameras;
    public ObservableCollection<MediaDevice> Speakers => SettingsVM.Speakers;
    public ObservableCollection<AgentInfo> Agents { get; } = new();
    public ObservableCollection<ConversationSession> Sessions { get; } = new();

    private bool _isListening;
    public bool IsListening { get => _isListening; set => SetProperty(ref _isListening, value); }
    private float _voiceLevel;
    public float VoiceLevel { get => _voiceLevel; set => SetProperty(ref _voiceLevel, value); }
    private bool _isBackendAvailable;
    public bool IsBackendAvailable { get => _isBackendAvailable; set => SetProperty(ref _isBackendAvailable, value); }
    private string _statusMessage = "Iniciando...";
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    private string _currentScreen = "chat";
    public string CurrentScreen { get => _currentScreen; set { if (SetProperty(ref _currentScreen, value)) { OnPropertyChanged(nameof(IsChatVisible)); OnPropertyChanged(nameof(IsDashboardVisible)); OnPropertyChanged(nameof(IsPoliciesVisible)); OnPropertyChanged(nameof(IsEpisodesVisible)); OnPropertyChanged(nameof(IsMemoryVisible)); OnPropertyChanged(nameof(IsSettingsVisible)); OnPropertyChanged(nameof(IsBenchmarkVisible)); OnPropertyChanged(nameof(IsCausalVisible)); OnPropertyChanged(nameof(IsProfileVisible)); OnPropertyChanged(nameof(IsDocumentsVisible)); OnPropertyChanged(nameof(IsArchiveVisible)); OnPropertyChanged(nameof(IsModelRegistryVisible)); OnPropertyChanged(nameof(IsVersionsVisible)); OnPropertyChanged(nameof(IsSessionsVisible)); } } }
    public bool IsChatVisible => _currentScreen == "chat";
    public bool IsDashboardVisible => _currentScreen == "dashboard";
    public bool IsPoliciesVisible => _currentScreen == "policies";
    public bool IsEpisodesVisible => _currentScreen == "episodes";
    public bool IsMemoryVisible => _currentScreen == "memory";
    public bool IsSettingsVisible => _currentScreen == "settings";
    public bool IsBenchmarkVisible => _currentScreen == "benchmark";
    public bool IsCausalVisible => _currentScreen == "causal";
    public bool IsProfileVisible => _currentScreen == "profile";
    public bool IsDocumentsVisible => _currentScreen == "documents";
    public bool IsArchiveVisible => _currentScreen == "archive";
    public bool IsModelRegistryVisible => _currentScreen == "modelregistry";
    public bool IsVersionsVisible => _currentScreen == "versions";
    public bool IsSessionsVisible => _currentScreen == "sessions";

    public AgentInfo? SelectedAgent { get; set; }
    private ConversationSession? _activeSession;
    public ConversationSession? ActiveSession { get => _activeSession; set => SetProperty(ref _activeSession, value); }

    // Theme
    // Mode
    private string _executionMode = "auto";
    public string ExecutionMode { get => _executionMode; set => SetProperty(ref _executionMode, value); }
    public bool IsModeAuto { get => _executionMode == "auto"; set { if (value) ExecutionMode = "auto"; OnPropertyChanged(); } }
    public bool IsModeSemi { get => _executionMode == "semi"; set { if (value) ExecutionMode = "semi"; OnPropertyChanged(); } }
    public bool IsModeManual { get => _executionMode == "manual"; set { if (value) ExecutionMode = "manual"; OnPropertyChanged(); } }

    private string _themeIcon = "☀️";
    public string ThemeIcon { get => _themeIcon; set => SetProperty(ref _themeIcon, value); }
    private string _themeLabel = "Modo claro";
    public string ThemeLabel { get => _themeLabel; set => SetProperty(ref _themeLabel, value); }

    // Video call
    private bool _isInVideoCall;
    public bool IsInVideoCall { get => _isInVideoCall; set => SetProperty(ref _isInVideoCall, value); }
    private string _videoCallState = "Disconnected";
    public string VideoCallState { get => _videoCallState; set { if (SetProperty(ref _videoCallState, value)) OnPropertyChanged(nameof(VideoCallStateText)); } }
    public string VideoCallStateText => VideoCallState switch { "Connected" => "Conectado", "Connecting" => "Conectando...", _ => "Desconectado" };
    private bool _isVideoCallMuted;
    public bool IsVideoCallMuted { get => _isVideoCallMuted; set { if (SetProperty(ref _isVideoCallMuted, value)) OnPropertyChanged(nameof(VideoCallMuteIcon)); } }
    public string VideoCallMuteIcon => IsVideoCallMuted ? "🔇" : "🔊";
    private bool _isVideoCallCameraOn = true;
    public bool IsVideoCallCameraOn { get => _isVideoCallCameraOn; set { if (SetProperty(ref _isVideoCallCameraOn, value)) OnPropertyChanged(nameof(VideoCallCameraIcon)); } }
    public string VideoCallCameraIcon => IsVideoCallCameraOn ? "📹" : "📵";

    public string ApiEndpoint => _settingsService.LoadSettings().ApiEndpoint ?? "http://localhost:5000";

    public ICommand NavigateToChatCommand { get; }
    public ICommand NavigateToDashboardCommand { get; }
    public ICommand NavigateToPoliciesCommand { get; }
    public ICommand NavigateToEpisodesCommand { get; }
    public ICommand NavigateToMemoryCommand { get; }
    public ICommand NavigateToSettingsCommand { get; }
    public ICommand NavigateToBenchmarkCommand { get; }
    public ICommand NavigateToCausalCommand { get; }
    public ICommand NavigateToDocumentsCommand { get; }
    public ICommand NavigateToArchiveCommand { get; }
    public ICommand NavigateToModelRegistryCommand { get; }
    public ICommand NavigateToVersionsCommand { get; }
    public ICommand NavigateToSessionsCommand { get; }
    public ICommand NavigateToProfileCommand { get; }
    public ICommand ToggleListeningCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand NewSessionCommand { get; }
    public ICommand RefreshDevicesCommand { get; }
    public ICommand RenameSessionCommand { get; }
    public ICommand DeleteSessionCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand ToggleVideoCallMuteCommand { get; }
    public ICommand ToggleVideoCallCameraCommand { get; }
    public ICommand EndVideoCallCommand { get; }

    public event EventHandler? LogoutRequested;

    public MainViewModel()
        : this(
            ServiceLocator.Instance.KernelClient,
            ServiceLocator.Instance.SettingsService,
            ServiceLocator.Instance.ThemeService,
            ServiceLocator.Instance.ListeningService,
            ServiceLocator.Instance.LocalizationService,
            ServiceLocator.Instance.GetLogger<MainViewModel>(),
            new ChatViewModel(), new DashboardViewModel(), new SettingsViewModel(),
            new MemoryViewModel(), new EpisodesViewModel(), new DocumentViewModel(),
            new PoliciesViewModel(), new BenchmarkViewModel(), new CausalGraphViewModel(),
            new ProfileViewModel(), new ArchiveViewModel(), new ModelRegistryViewModel(),
            new VersionsViewModel(), new SessionsViewModel())
    { }

    public MainViewModel(
        IKernelClient kernelClient,
        ISettingsService settingsService,
        IThemeService themeService,
        IListeningService listeningService,
        ILocalizationService localizationService,
        ILogger<MainViewModel> logger,
        ChatViewModel chatVM, DashboardViewModel dashVM, SettingsViewModel settingsVM,
        MemoryViewModel memoryVM, EpisodesViewModel episodesVM, DocumentViewModel documentVM,
        PoliciesViewModel policiesVM, BenchmarkViewModel benchmarkVM, CausalGraphViewModel causalVM,
        ProfileViewModel profileVM, ArchiveViewModel archiveVM, ModelRegistryViewModel modelRegistryVM,
        VersionsViewModel versionsVM, SessionsViewModel sessionsVM)
    {
        ChatVM = chatVM;
        DashVM = dashVM;
        SettingsVM = settingsVM;
        MemoryVM = memoryVM;
        EpisodesVM = episodesVM;
        DocumentVM = documentVM;
        PoliciesVM = policiesVM;
        BenchmarkVM = benchmarkVM;
        CausalVM = causalVM;
        ProfileVM = profileVM;
        ArchiveVM = archiveVM;
        ModelRegistryVM = modelRegistryVM;
        VersionsVM = versionsVM;
        SessionsVM = sessionsVM;

        _kernelClient = kernelClient;
        _settingsService = settingsService;
        _themeService = themeService;
        _listeningService = listeningService;
        _localizationService = localizationService;
        _logger = logger;

        Agents.Add(new AgentInfo("gateway", "Gateway", "Modo Gateway - acesso completo"));
        Agents.Add(new AgentInfo("kernel", "KrnlAI", "Modo KrnlAI - decisões locais"));
        SelectedAgent = Agents[0];
        Sessions.Add(new ConversationSession("default", "Conversa 1", DateTime.Now));
        ActiveSession = Sessions[0];

        NavigateToChatCommand = new RelayCommand(() => CurrentScreen = "chat");
        NavigateToDashboardCommand = new RelayCommand(() => CurrentScreen = "dashboard");
        NavigateToPoliciesCommand = new RelayCommand(() => CurrentScreen = "policies");
        NavigateToEpisodesCommand = new RelayCommand(() => CurrentScreen = "episodes");
        NavigateToMemoryCommand = new RelayCommand(() => CurrentScreen = "memory");
        NavigateToSettingsCommand = new RelayCommand(() => CurrentScreen = "settings");
        NavigateToBenchmarkCommand = new RelayCommand(() => CurrentScreen = "benchmark");
        NavigateToCausalCommand = new RelayCommand(() => CurrentScreen = "causal");
        NavigateToDocumentsCommand = new RelayCommand(() => CurrentScreen = "documents");
        NavigateToArchiveCommand = new RelayCommand(() => CurrentScreen = "archive");
        NavigateToModelRegistryCommand = new RelayCommand(() => CurrentScreen = "modelregistry");
        NavigateToVersionsCommand = new RelayCommand(() => CurrentScreen = "versions");
        NavigateToSessionsCommand = new RelayCommand(() => CurrentScreen = "sessions");
        NavigateToProfileCommand = new RelayCommand(() => CurrentScreen = "profile");
        ToggleListeningCommand = new AsyncRelayCommand(ToggleListeningAsync);
        LogoutCommand = new RelayCommand(ExecuteLogout);
        NewSessionCommand = new RelayCommand(() => { Sessions.Add(new ConversationSession(Guid.NewGuid().ToString(), $"Conversa {Sessions.Count + 1}", DateTime.Now)); ActiveSession = Sessions.Last(); });
        RefreshDevicesCommand = new RelayCommand(() => SettingsVM.LoadDevices());
        RenameSessionCommand = new RelayCommand(() => { if (ActiveSession != null) { var i = Sessions.IndexOf(ActiveSession); Sessions[i] = new ConversationSession(ActiveSession.Id, ActiveSession.Title + " *", ActiveSession.CreatedAt); } });
        DeleteSessionCommand = new RelayCommand(() => { if (ActiveSession != null && Sessions.Count > 1) { Sessions.Remove(ActiveSession); ActiveSession = Sessions.FirstOrDefault(); } });
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        ToggleVideoCallMuteCommand = new RelayCommand(() => IsVideoCallMuted = !IsVideoCallMuted);
        ToggleVideoCallCameraCommand = new RelayCommand(() => IsVideoCallCameraOn = !IsVideoCallCameraOn);
        EndVideoCallCommand = new RelayCommand(() => IsInVideoCall = false);

        _listeningService.VoiceLevelChanged += OnVoiceLevelChanged;
        _themeService.ThemeChanged += OnThemeChanged;
        UpdateThemeDisplay(_themeService.CurrentTheme);
        _ = CheckBackendHealthAsync();
        _ = PollEmotionalStateAsync();
    }

    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
    }

    private void OnThemeChanged(object? sender, string themeName)
    {
        UiThreadInvoker.Invoke(() => UpdateThemeDisplay(themeName));
    }

    private void UpdateThemeDisplay(string theme)
    {
        if (theme == "dark")
        {
            ThemeIcon = "☀️";
            ThemeLabel = "Modo claro";
        }
        else
        {
            ThemeIcon = "🌙";
            ThemeLabel = "Modo escuro";
        }
    }

    private async Task ToggleListeningAsync()
    {
        if (IsListening) { await _listeningService.StopListeningAsync(); IsListening = false; StatusMessage = "Escuta parada"; }
        else { await _listeningService.StartListeningAsync(); IsListening = true; StatusMessage = "Escutando..."; }
    }

    private async Task CheckBackendHealthAsync()
    {
        _healthCheckCts = new CancellationTokenSource();
        var t = _healthCheckCts.Token;
        while (!t.IsCancellationRequested)
        {
            var isAvailable = await _kernelClient.CheckHealthAsync(t);
            UiThreadInvoker.Invoke(() =>
            {
                IsBackendAvailable = isAvailable;
                StatusMessage = IsBackendAvailable ? "Conectado" : "Servidor indisponível";
                OnPropertyChanged(nameof(ApiEndpoint));
            });
            try { await Task.Delay(30000, t); } catch (OperationCanceledException) { break; }
        }
    }

    private async Task PollEmotionalStateAsync()
    {
        _emotionalPollCts = new CancellationTokenSource();
        var t = _emotionalPollCts.Token;
        while (!t.IsCancellationRequested)
        {
            if (IsBackendAvailable)
            {
                var state = await _kernelClient.GetEmotionalStateAsync(UserId, t);
                if (state != null)
                {
                    UiThreadInvoker.Invoke(() => EmotionalState = state);
                }
            }
            try { await Task.Delay(15000, t); } catch (OperationCanceledException) { break; }
        }
    }

    private void ExecuteLogout()
    {
        var settings = _settingsService.LoadSettings();
        _settingsService.SaveSettings(settings with { AuthToken = null, IsAuthenticated = false });
        _kernelClient.SetAuthToken(null);
        LogoutRequested?.Invoke(this, EventArgs.Empty);
    }

    public void StopHealthCheck()
    {
        _healthCheckCts?.Cancel();
        _healthCheckCts?.Dispose();
        _healthCheckCts = null;
        _emotionalPollCts?.Cancel();
        _emotionalPollCts?.Dispose();
        _emotionalPollCts = null;
    }

    public void Cleanup()
    {
        StopHealthCheck();
        _listeningService.VoiceLevelChanged -= OnVoiceLevelChanged;
        _themeService.ThemeChanged -= OnThemeChanged;
        ChatVM.Cleanup();
    }

    private void OnVoiceLevelChanged(object? sender, float level) => UiThreadInvoker.Invoke(() => VoiceLevel = level);
}
