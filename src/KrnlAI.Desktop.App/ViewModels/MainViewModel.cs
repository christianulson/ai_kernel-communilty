using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Desktop.Core.Services;
using Microsoft.Extensions.Logging;


namespace KrnlAI.Desktop.App.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IKernelClient? _kernelClient;
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private readonly IListeningService? _listeningService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<MainViewModel> _logger;
    private CancellationTokenSource? _healthCheckCts;
    private CancellationTokenSource? _emotionalPollCts;
    private CancellationTokenSource? _dashboardRefreshCts;
    private EmotionalState? _emotionalState;
    private string? _updateVersion;
    public string? UpdateVersion { get => _updateVersion; set { SetProperty(ref _updateVersion, value); OnPropertyChanged(nameof(IsUpdateAvailable)); } }
    public bool IsUpdateAvailable => _updateVersion != null;
    private readonly System.ComponentModel.PropertyChangedEventHandler? _chatPropertyChangedHandler;
    private string _executiveMode = "auto";
    public string ExecutiveMode { get => _executiveMode; set => SetProperty(ref _executiveMode, value); }
    public string ExecutiveModeIcon => _executiveMode switch { "focus" => "🎯", "deep" => "🧠", "sleep" => "💤", "crisis" => "🚨", _ => "⚡" };
    private bool _previousBackendAvailable = true;
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
    public UserServicesViewModel UserServicesVM { get; private set; }
    public ApiKeysViewModel ApiKeysVM { get; }
    public PeerRankingViewModel PeerRankingVM { get; }
    public ArchiveViewModel ArchiveVM { get; }
    public ModelRegistryViewModel ModelRegistryVM { get; }
    public VersionsViewModel VersionsVM { get; }
    public SessionsViewModel SessionsVM { get; }
    public KanbanViewModel KanbanVM { get; }
    public TrajectoryViewerViewModel TrajectoryVM { get; } = new();
    public P2PPaymentsViewModel P2PVM { get; }
    public DisputesViewModel DisputesVM { get; }
    public SidecarViewModel SidecarVM { get; }
    public ObjectivesViewModel ObjectivesVM { get; }
    public InvestigationsViewModel InvestigationsVM { get; }
    public SnapshotsViewModel SnapshotsVM { get; }
    public AdminConfigViewModel AdminConfigVM { get; }
    public AdminUsersViewModel AdminUsersVM { get; }
    public MomentsViewModel MomentsVM { get; }
    public PluginsViewModel PluginsVM { get; }
    public SchedulerViewModel SchedulerVM { get; }
    public ApprovalViewModel ApprovalVM { get; }
    public TemplatesViewModel TemplatesVM { get; }
    public CliViewModel CliVM { get; } = new();
    public PluginCatalogViewModel PluginCatalogVM { get; } = new();
    public ExperimentsViewModel ExperimentsVM { get; }
    public InitWizardViewModel InitWizardVM { get; } = new();
    public DebugViewModel DebugVM { get; } = new();
    public CognitiveStudioViewModel StudioVM { get; } = new();
    public WelcomeWizardViewModel WelcomeVM { get; } = new();
    public AgiDashboardViewModel AgiVM { get; }
    public KnowledgeViewModel KnowledgeVM { get; }
    public PieViewModel PieVM { get; }
    public EmotionalViewModel EmotionalVM { get; }
    public EventsViewModel EventsVM { get; }
    public CodingViewModel CodingVM { get; }
    public SelfImprovementViewModel SelfImprovementVM { get; }
    public AssistantViewModel AssistantVM { get; }
    public McpConfigViewModel McpConfigVM { get; }
    public PlanViewModel PlanVM { get; }
    public FeedbackViewModel FeedbackVM { get; }
    public EpisodicMemoryViewModel EpisodicMemoryVM { get; }
    public CreativityViewModel CreativityVM { get; }
    public CognitiveCycleViewModel CognitiveCycleVM { get; }

    public ObservableCollection<MediaDevice> Microphones => SettingsVM.Microphones;
    public ObservableCollection<MediaDevice> Cameras => SettingsVM.Cameras;
    public ObservableCollection<MediaDevice> Speakers => SettingsVM.Speakers;
    public ObservableCollection<AgentInfo> Agents { get; } = [];
    public ObservableCollection<ConversationSession> Sessions { get; } = [];
    public ObservableCollection<ConversationSession> FilteredSessions { get; } = [];
    private string _sessionFilter = "";
    public string SessionFilter { get => _sessionFilter; set { if (SetProperty(ref _sessionFilter, value)) ApplySessionFilter(); } }
    private void ApplySessionFilter()
    {
        FilteredSessions.Clear();
        var filtered = string.IsNullOrWhiteSpace(_sessionFilter)
            ? Sessions
            : new ObservableCollection<ConversationSession>(Sessions.Where(s => s.Title.Contains(_sessionFilter, StringComparison.OrdinalIgnoreCase)));
        foreach (var s in filtered) FilteredSessions.Add(s);
        if (ActiveSession != null && !FilteredSessions.Contains(ActiveSession))
            ActiveSession = FilteredSessions.FirstOrDefault();
    }

    private bool _isListening;
    public bool IsListening { get => _isListening; set => SetProperty(ref _isListening, value); }
    public bool IsAgentProcessing => ChatVM?.IsProcessing ?? false;
    private float _voiceLevel;
    public float VoiceLevel { get => _voiceLevel; set => SetProperty(ref _voiceLevel, value); }
    private bool _isBackendAvailable;
    public bool IsBackendAvailable { get => _isBackendAvailable; set => SetProperty(ref _isBackendAvailable, value); }
    private string _statusMessage = "Iniciando...";
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    private string _currentScreen = "chat";
    public string CurrentScreen { get => _currentScreen; set { if (SetProperty(ref _currentScreen, value)) { OnPropertyChanged(nameof(IsChatVisible)); OnPropertyChanged(nameof(IsDashboardVisible)); OnPropertyChanged(nameof(IsPoliciesVisible)); OnPropertyChanged(nameof(IsEpisodesVisible)); OnPropertyChanged(nameof(IsMemoryVisible)); OnPropertyChanged(nameof(IsSettingsVisible)); OnPropertyChanged(nameof(IsApiKeysVisible)); OnPropertyChanged(nameof(IsPeerRankingVisible)); OnPropertyChanged(nameof(IsPrivacyVisible)); OnPropertyChanged(nameof(IsUserServicesVisible)); OnPropertyChanged(nameof(IsBenchmarkVisible)); OnPropertyChanged(nameof(IsCausalVisible)); OnPropertyChanged(nameof(IsProfileVisible)); OnPropertyChanged(nameof(IsDocumentsVisible)); OnPropertyChanged(nameof(IsArchiveVisible)); OnPropertyChanged(nameof(IsModelRegistryVisible)); OnPropertyChanged(nameof(IsVersionsVisible)); OnPropertyChanged(nameof(IsSessionsVisible)); OnPropertyChanged(nameof(IsKanbanVisible)); OnPropertyChanged(nameof(IsTrajectoryVisible)); OnPropertyChanged(nameof(IsP2PPaymentsVisible)); OnPropertyChanged(nameof(IsDisputesVisible));             OnPropertyChanged(nameof(IsSidecarVisible)); OnPropertyChanged(nameof(IsMultimodalVisible)); OnPropertyChanged(nameof(IsObjectivesVisible)); OnPropertyChanged(nameof(IsInvestigationsVisible)); OnPropertyChanged(nameof(IsSnapshotsVisible)); OnPropertyChanged(nameof(IsAdminConfigVisible));             OnPropertyChanged(nameof(IsAdminUsersVisible)); OnPropertyChanged(nameof(IsMomentsVisible)); OnPropertyChanged(nameof(IsPluginsVisible)); OnPropertyChanged(nameof(IsSchedulerVisible)); OnPropertyChanged(nameof(IsTemplatesVisible)); OnPropertyChanged(nameof(IsApprovalVisible));                     OnPropertyChanged(nameof(IsCliVisible)); OnPropertyChanged(nameof(IsTerminalVisible)); OnPropertyChanged(nameof(IsPluginCatalogVisible)); OnPropertyChanged(nameof(IsExperimentVisible)); OnPropertyChanged(nameof(IsInitWizardVisible)); OnPropertyChanged(nameof(IsDebugVisible)); OnPropertyChanged(nameof(IsStudioVisible)); OnPropertyChanged(nameof(IsAgiVisible)); OnPropertyChanged(nameof(IsKnowledgeVisible)); OnPropertyChanged(nameof(IsPieVisible)); OnPropertyChanged(nameof(IsEmotionalVisible)); OnPropertyChanged(nameof(IsEventsVisible)); OnPropertyChanged(nameof(IsCodingVisible)); OnPropertyChanged(nameof(IsSelfImprovementVisible)); OnPropertyChanged(nameof(IsAssistantVisible)); OnPropertyChanged(nameof(IsMcpConfigVisible)); OnPropertyChanged(nameof(IsPlanVisible)); OnPropertyChanged(nameof(IsFeedbackVisible)); OnPropertyChanged(nameof(IsEpisodicMemoryVisible)); OnPropertyChanged(nameof(IsCreativityVisible)); OnPropertyChanged(nameof(IsCognitiveCycleVisible)); } } }
    private bool _showWelcomeWizard = true;
    public bool ShowWelcomeWizard { get => _showWelcomeWizard; set => SetProperty(ref _showWelcomeWizard, value); }
    private bool _showSearch;
    public bool ShowSearch { get => _showSearch; set => SetProperty(ref _showSearch, value); }
    private bool _showCommandPalette;
    public bool ShowCommandPalette { get => _showCommandPalette; set => SetProperty(ref _showCommandPalette, value); }
    private bool _showLogs;
    public bool ShowLogs { get => _showLogs; set => SetProperty(ref _showLogs, value); }
    private bool _kioskMode;
    public bool KioskMode { get => _kioskMode; set => SetProperty(ref _kioskMode, value); }
    private int _unreadCount;
    public int UnreadCount { get => _unreadCount; set => SetProperty(ref _unreadCount, value); }
    private bool _showShortcuts;
    public bool ShowShortcuts { get => _showShortcuts; set => SetProperty(ref _showShortcuts, value); }
    public bool IsChatVisible => _currentScreen == "chat";
    public bool IsDashboardVisible => _currentScreen == "dashboard";
    public bool IsPoliciesVisible => _currentScreen == "policies";
    public bool IsEpisodesVisible => _currentScreen == "episodes";
    public bool IsMemoryVisible => _currentScreen == "memory";
    public bool IsSettingsVisible => _currentScreen == "settings";
    public bool IsApiKeysVisible => _currentScreen == "api-keys";
    public bool IsPeerRankingVisible => _currentScreen == "peer-ranking";
    public bool IsPrivacyVisible => _currentScreen == "privacy";
    public bool IsUserServicesVisible => _currentScreen == "user-services";
    public bool IsBenchmarkVisible => _currentScreen == "benchmark";
    public bool IsCausalVisible => _currentScreen == "causal";
    public bool IsProfileVisible => _currentScreen == "profile";
    public bool IsDocumentsVisible => _currentScreen == "documents";
    public bool IsArchiveVisible => _currentScreen == "archive";
    public bool IsModelRegistryVisible => _currentScreen == "modelregistry";
    public bool IsVersionsVisible => _currentScreen == "versions";
    public bool IsSessionsVisible => _currentScreen == "sessions";
    public bool IsKanbanVisible => _currentScreen == "kanban";
    public bool IsTrajectoryVisible => _currentScreen == "trajectory";
    public bool IsP2PPaymentsVisible => _currentScreen == "p2p-payments";
    public bool IsDisputesVisible => _currentScreen == "disputes";
    public bool IsSidecarVisible => _currentScreen == "sidecar";
    public bool IsMultimodalVisible => _currentScreen == "multimodal";
    public bool IsObjectivesVisible => _currentScreen == "objectives";
    public bool IsInvestigationsVisible => _currentScreen == "investigations";
    public bool IsSnapshotsVisible => _currentScreen == "snapshots";
    public bool IsAdminConfigVisible => _currentScreen == "admin-config";
    public bool IsAdminUsersVisible => _currentScreen == "admin-users";
    public bool IsMomentsVisible => _currentScreen == "moments";
    public bool IsPluginsVisible => _currentScreen == "plugins";
    public bool IsSchedulerVisible => _currentScreen == "scheduler";
    public bool IsTemplatesVisible => _currentScreen == "templates";
    public bool IsApprovalVisible => _currentScreen == "approvals";
    public bool IsInitWizardVisible => _currentScreen == "init";
    public bool IsDebugVisible => _currentScreen == "debug";
    public bool IsStudioVisible => _currentScreen == "studio";
    public bool IsCliVisible => _currentScreen == "cli";
    public bool IsTerminalVisible => _currentScreen == "terminal";
    public bool IsPluginCatalogVisible => _currentScreen == "plugin-catalog";
    public bool IsExperimentVisible => _currentScreen == "experiment";
    public bool IsAgiVisible => _currentScreen == "agi";
    public bool IsKnowledgeVisible => _currentScreen == "knowledge";
    public bool IsPieVisible => _currentScreen == "pie";
    public bool IsEmotionalVisible => _currentScreen == "emotional";
    public bool IsEventsVisible => _currentScreen == "events";
    public bool IsCodingVisible => _currentScreen == "coding";
    public bool IsSelfImprovementVisible => _currentScreen == "self-improvement";
    public bool IsAssistantVisible => _currentScreen == "assistant";
    public bool IsMcpConfigVisible => _currentScreen == "mcp-config";
    public bool IsPlanVisible => _currentScreen == "plan";
    public bool IsFeedbackVisible => _currentScreen == "feedback";
    public bool IsEpisodicMemoryVisible => _currentScreen == "episodic-memory";
    public bool IsCreativityVisible => _currentScreen == "creativity";
    public bool IsCognitiveCycleVisible => _currentScreen == "cognitive-cycle";

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

    public string ApiEndpoint
    {
        get
        {
            var s = _settingsService.LoadSettings();
            return s.ApiEndpoint ?? s.ApiBaseUrl ?? "http://localhost:5235";
        }
    }

    public ICommand NavigateToChatCommand { get; }
    public ICommand NavigateToDashboardCommand { get; }
    public ICommand NavigateToPoliciesCommand { get; }
    public ICommand NavigateToEpisodesCommand { get; }
    public ICommand NavigateToMemoryCommand { get; }
    public ICommand NavigateToSettingsCommand { get; }
    public ICommand NavigateToApiKeysCommand { get; }
    public ICommand NavigateToPeerRankingCommand { get; }
    public ICommand NavigateToPrivacyCommand { get; }
    public ICommand NavigateToUserServicesCommand { get; }
    public ICommand NavigateToBenchmarkCommand { get; }
    public ICommand NavigateToCausalCommand { get; }
    public ICommand NavigateToDocumentsCommand { get; }
    public ICommand NavigateToArchiveCommand { get; }
    public ICommand NavigateToModelRegistryCommand { get; }
    public ICommand NavigateToVersionsCommand { get; }
    public ICommand NavigateToSessionsCommand { get; }
    public ICommand NavigateToKanbanCommand { get; }
    public ICommand NavigateToTrajectoryCommand { get; }
    public ICommand NavigateToP2PPaymentsCommand { get; }
    public ICommand NavigateToDisputesCommand { get; }
    public ICommand NavigateToSidecarCommand { get; }
    public ICommand NavigateToMultimodalCommand { get; }
    public ICommand NavigateToObjectivesCommand { get; }
    public ICommand NavigateToInvestigationsCommand { get; }
    public ICommand NavigateToSnapshotsCommand { get; }
    public ICommand NavigateToAdminConfigCommand { get; }
    public ICommand NavigateToAdminUsersCommand { get; }
    public ICommand NavigateToMomentsCommand { get; }
    public ICommand NavigateToPluginsCommand { get; }
    public ICommand NavigateToSchedulerCommand { get; }
    public ICommand NavigateToTemplatesCommand { get; }
    public ICommand NavigateToApprovalsCommand { get; }
    public ICommand NavigateToInitWizardCommand { get; }
    public ICommand NavigateToDebugCommand { get; }
    public ICommand NavigateToStudioCommand { get; }
    public ICommand NavigateToCliCommand { get; }
    public ICommand NavigateToTerminalCommand { get; }
    public ICommand NavigateToPluginCatalogCommand { get; }
    public ICommand NavigateToExperimentCommand { get; }
    public ICommand NavigateToAgiCommand { get; }
    public ICommand NavigateToKnowledgeCommand { get; }
    public ICommand NavigateToPieCommand { get; }
    public ICommand NavigateToEmotionalCommand { get; }
    public ICommand NavigateToEventsCommand { get; }
    public ICommand NavigateToCodingCommand { get; }
    public ICommand NavigateToSelfImprovementCommand { get; }
    public ICommand NavigateToAssistantCommand { get; }
    public ICommand NavigateToMcpConfigCommand { get; }
    public ICommand NavigateToPlanCommand { get; }
    public ICommand NavigateToFeedbackCommand { get; }
    public ICommand NavigateToEpisodicMemoryCommand { get; }
    public ICommand NavigateToCreativityCommand { get; }
    public ICommand NavigateToCognitiveCycleCommand { get; }
    public ICommand NavigateToProfileCommand { get; }
    public ICommand ToggleListeningCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand FeedbackCommand { get; }
    public ICommand BackupCommand { get; }
    public ICommand RestoreCommand { get; }
    public ICommand NewSessionCommand { get; }
    public ICommand RefreshDevicesCommand { get; }
    public ICommand RenameSessionCommand { get; }
    public ICommand DeleteSessionCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand ToggleSearchCommand { get; }
    public ICommand ToggleCommandPaletteCommand { get; }
    public ICommand ToggleLogsCommand { get; }
    public ICommand ToggleKioskCommand { get; }
    public ICommand CheckUpdateCommand { get; }
    public ICommand DownloadUpdateCommand { get; }
    public ICommand StartSidecarCommand { get; }
    public ICommand ToggleShortcutsCommand { get; }
    public ICommand ToggleVideoCallMuteCommand { get; }
    public ICommand ToggleVideoCallCameraCommand { get; }
    public ICommand EndVideoCallCommand { get; }

    public event EventHandler? LogoutRequested;

    public string AuthenticationStatus => BuildAuthenticationStatus(_settingsService.LoadSettings());
    public string AuthenticationSource => BuildAuthenticationSource(_settingsService.LoadSettings());

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
            new ProfileViewModel(), new UserServicesViewModel(), new ApiKeysViewModel(), new PeerRankingViewModel(), new ArchiveViewModel(), new ModelRegistryViewModel(),
            new VersionsViewModel(), new SessionsViewModel(), new KanbanViewModel(),
            new P2PPaymentsViewModel(), new DisputesViewModel(), new SidecarViewModel(),
            new ObjectivesViewModel(), new InvestigationsViewModel(), new SnapshotsViewModel(),
            new AdminConfigViewModel(), new AdminUsersViewModel(),
            new MomentsViewModel(), new PluginsViewModel(), new SchedulerViewModel(),
            new ApprovalViewModel(), new AgiDashboardViewModel(),
            new KnowledgeViewModel(), new PieViewModel(), new EmotionalViewModel(), new EventsViewModel(),
            new CodingViewModel(), new SelfImprovementViewModel(), new AssistantViewModel(), new McpConfigViewModel(),
            new TemplatesViewModel(), new ExperimentsViewModel(),
            new PlanViewModel(), new FeedbackViewModel(), new EpisodicMemoryViewModel(),
            new CreativityViewModel(), new CognitiveCycleViewModel())
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
        ProfileViewModel profileVM, UserServicesViewModel userServicesVM, ApiKeysViewModel apiKeysVM, PeerRankingViewModel peerRankingVM, ArchiveViewModel archiveVM, ModelRegistryViewModel modelRegistryVM,
        VersionsViewModel versionsVM, SessionsViewModel sessionsVM, KanbanViewModel kanbanVM,
        P2PPaymentsViewModel p2pVM, DisputesViewModel disputesVM, SidecarViewModel sidecarVM,
        ObjectivesViewModel objectivesVM, InvestigationsViewModel investigationsVM, SnapshotsViewModel snapshotsVM,
        AdminConfigViewModel adminConfigVM, AdminUsersViewModel adminUsersVM,
        MomentsViewModel momentsVM, PluginsViewModel pluginsVM, SchedulerViewModel schedulerVM,
        ApprovalViewModel approvalVM, AgiDashboardViewModel agiVM,
        KnowledgeViewModel knowledgeVM, PieViewModel pieVM, EmotionalViewModel emotionalVM, EventsViewModel eventsVM,
        CodingViewModel codingVM, SelfImprovementViewModel selfImprovementVM, AssistantViewModel assistantVM, McpConfigViewModel mcpConfigVM,
        TemplatesViewModel templatesVM, ExperimentsViewModel experimentsVM,
        PlanViewModel planVM, FeedbackViewModel feedbackVM, EpisodicMemoryViewModel episodicMemoryVM,
        CreativityViewModel creativityVM, CognitiveCycleViewModel cognitiveCycleVM)
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
        UserServicesVM = userServicesVM;
        ApiKeysVM = apiKeysVM;
        PeerRankingVM = peerRankingVM;
        ArchiveVM = archiveVM;
        ModelRegistryVM = modelRegistryVM;
        VersionsVM = versionsVM;
        SessionsVM = sessionsVM;
        KanbanVM = kanbanVM;
        P2PVM = p2pVM;
        DisputesVM = disputesVM;
        SidecarVM = sidecarVM;
        ObjectivesVM = objectivesVM;
        InvestigationsVM = investigationsVM;
        SnapshotsVM = snapshotsVM;
        AdminConfigVM = adminConfigVM;
        AdminUsersVM = adminUsersVM;
        MomentsVM = momentsVM;
        PluginsVM = pluginsVM;
        SchedulerVM = schedulerVM;
        ApprovalVM = approvalVM;
        AgiVM = agiVM;
        KnowledgeVM = knowledgeVM;
        PieVM = pieVM;
        EmotionalVM = emotionalVM;
        EventsVM = eventsVM;
        CodingVM = codingVM;
        SelfImprovementVM = selfImprovementVM;
        AssistantVM = assistantVM;
        McpConfigVM = mcpConfigVM;
        TemplatesVM = templatesVM;
        ExperimentsVM = experimentsVM;
        PlanVM = planVM;
        FeedbackVM = feedbackVM;
        EpisodicMemoryVM = episodicMemoryVM;
        CreativityVM = creativityVM;
        CognitiveCycleVM = cognitiveCycleVM;

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
        NavigateToApiKeysCommand = new RelayCommand(() => CurrentScreen = "api-keys");
        NavigateToPeerRankingCommand = new RelayCommand(() => CurrentScreen = "peer-ranking");
        NavigateToPrivacyCommand = new RelayCommand(() => CurrentScreen = "privacy");
        NavigateToUserServicesCommand = new RelayCommand(() => CurrentScreen = "user-services");
        NavigateToBenchmarkCommand = new RelayCommand(() => CurrentScreen = "benchmark");
        NavigateToCausalCommand = new RelayCommand(() => CurrentScreen = "causal");
        NavigateToDocumentsCommand = new RelayCommand(() => CurrentScreen = "documents");
        NavigateToArchiveCommand = new RelayCommand(() => CurrentScreen = "archive");
        NavigateToModelRegistryCommand = new RelayCommand(() => CurrentScreen = "modelregistry");
        NavigateToVersionsCommand = new RelayCommand(() => CurrentScreen = "versions");
        NavigateToSessionsCommand = new RelayCommand(() => CurrentScreen = "sessions");
        NavigateToKanbanCommand = new RelayCommand(() => CurrentScreen = "kanban");
        NavigateToProfileCommand = new RelayCommand(() => CurrentScreen = "profile");
        NavigateToTrajectoryCommand = new RelayCommand(() => CurrentScreen = "trajectory");
        NavigateToP2PPaymentsCommand = new RelayCommand(() => CurrentScreen = "p2p-payments");
        NavigateToDisputesCommand = new RelayCommand(() => CurrentScreen = "disputes");
        NavigateToSidecarCommand = new RelayCommand(() => CurrentScreen = "sidecar");
        NavigateToMultimodalCommand = new RelayCommand(() => CurrentScreen = "multimodal");
        NavigateToObjectivesCommand = new RelayCommand(() => CurrentScreen = "objectives");
        NavigateToInvestigationsCommand = new RelayCommand(() => CurrentScreen = "investigations");
        NavigateToSnapshotsCommand = new RelayCommand(() => CurrentScreen = "snapshots");
        NavigateToAdminConfigCommand = new RelayCommand(() => CurrentScreen = "admin-config");
        NavigateToAdminUsersCommand = new RelayCommand(() => CurrentScreen = "admin-users");
        NavigateToMomentsCommand = new RelayCommand(() => CurrentScreen = "moments");
        NavigateToPluginsCommand = new RelayCommand(() => CurrentScreen = "plugins");
        NavigateToSchedulerCommand = new RelayCommand(() => CurrentScreen = "scheduler");
        NavigateToTemplatesCommand = new RelayCommand(() => CurrentScreen = "templates");
        NavigateToApprovalsCommand = new RelayCommand(() => CurrentScreen = "approvals");
        NavigateToInitWizardCommand = new RelayCommand(() => CurrentScreen = "init");
        NavigateToDebugCommand = new RelayCommand(() => CurrentScreen = "debug");
        NavigateToStudioCommand = new RelayCommand(() => CurrentScreen = "studio");
        NavigateToCliCommand = new RelayCommand(() => CurrentScreen = "cli");
        NavigateToTerminalCommand = new RelayCommand(() => CurrentScreen = "terminal");
        NavigateToPluginCatalogCommand = new RelayCommand(() => CurrentScreen = "plugin-catalog");
        NavigateToExperimentCommand = new RelayCommand(() => CurrentScreen = "experiment");
        NavigateToAgiCommand = new RelayCommand(() => CurrentScreen = "agi");
        NavigateToKnowledgeCommand = new RelayCommand(() => CurrentScreen = "knowledge");
        NavigateToPieCommand = new RelayCommand(() => CurrentScreen = "pie");
        NavigateToEmotionalCommand = new RelayCommand(() => CurrentScreen = "emotional");
        NavigateToEventsCommand = new RelayCommand(() => CurrentScreen = "events");
        NavigateToCodingCommand = new RelayCommand(() => CurrentScreen = "coding");
        NavigateToSelfImprovementCommand = new RelayCommand(() => CurrentScreen = "self-improvement");
        NavigateToAssistantCommand = new RelayCommand(() => CurrentScreen = "assistant");
        NavigateToMcpConfigCommand = new RelayCommand(() => CurrentScreen = "mcp-config");
        NavigateToPlanCommand = new RelayCommand(() => CurrentScreen = "plan");
        NavigateToFeedbackCommand = new RelayCommand(() => CurrentScreen = "feedback");
        NavigateToEpisodicMemoryCommand = new RelayCommand(() => CurrentScreen = "episodic-memory");
        NavigateToCreativityCommand = new RelayCommand(() => CurrentScreen = "creativity");
        NavigateToCognitiveCycleCommand = new RelayCommand(() => CurrentScreen = "cognitive-cycle");
        ToggleListeningCommand = new AsyncRelayCommand(ToggleListeningAsync);
        LogoutCommand = new RelayCommand(ExecuteLogout);
        BackupCommand = new AsyncRelayCommand(async () =>
        {
            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Backup files (*.zip)|*.zip", FileName = $"krnlai-backup-{DateTime.Now:yyyyMMdd}.zip" };
            if (dialog.ShowDialog() == true)
                await new BackupService().BackupAsync(dialog.FileName).ConfigureAwait(false);
        });
        RestoreCommand = new AsyncRelayCommand(async () =>
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Backup files (*.zip)|*.zip" };
            if (dialog.ShowDialog() == true)
                await new BackupService().RestoreAsync(dialog.FileName).ConfigureAwait(false);
        });
        FeedbackCommand = new RelayCommand(() =>
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/krnlai/krnl-ai/issues/new") { UseShellExecute = true }); }
            catch (Exception ex) { KrnlLogger.Write($"Feedback: {ex.Message}"); }
        });
        NewSessionCommand = new RelayCommand(() => { Sessions.Add(new ConversationSession(Guid.NewGuid().ToString(), $"Conversa {Sessions.Count + 1}", DateTime.Now)); ActiveSession = Sessions.Last(); });
        RefreshDevicesCommand = new RelayCommand(() => SettingsVM.LoadDevices());
        RenameSessionCommand = new AsyncRelayCommand(RenameSessionAsync);
        DeleteSessionCommand = new RelayCommand(() => { if (ActiveSession != null && Sessions.Count > 1) { Sessions.Remove(ActiveSession); ActiveSession = Sessions.FirstOrDefault(); } });
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        ToggleSearchCommand = new RelayCommand(() => ShowSearch = !ShowSearch);
        ToggleCommandPaletteCommand = new RelayCommand(() => ShowCommandPalette = !ShowCommandPalette);
        ToggleLogsCommand = new RelayCommand(() => ShowLogs = !ShowLogs);
        ToggleKioskCommand = new RelayCommand(() => KioskMode = !KioskMode);
        CheckUpdateCommand = new AsyncRelayCommand(async () => UpdateVersion = await new UpdateChecker().CheckForUpdatesAsync().ConfigureAwait(false));
        DownloadUpdateCommand = new AsyncRelayCommand(async () =>
        {
            if (UpdateVersion != null)
                await new UpdateChecker().DownloadAndInstallAsync(UpdateVersion).ConfigureAwait(false);
        });
        StartSidecarCommand = new AsyncRelayCommand(async () =>
        {
            try
            {
                var sidecarPath = Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\..\\..\\..\\Community\\src\\KrnlAI.Sidecar\\bin\\Debug\\net10.0\\KrnlAI.Sidecar.exe");
                if (File.Exists(sidecarPath))
                    System.Diagnostics.Process.Start(sidecarPath);
                else
                    KrnlLogger.Write("Sidecar not found at: " + sidecarPath);
            }
            catch (Exception ex) { KrnlLogger.Write($"StartSidecar: {ex.Message}"); }
        });
        ToggleShortcutsCommand = new RelayCommand(() => ShowShortcuts = !ShowShortcuts);
        ToggleVideoCallMuteCommand = new RelayCommand(() => IsVideoCallMuted = !IsVideoCallMuted);
        ToggleVideoCallCameraCommand = new RelayCommand(() => IsVideoCallCameraOn = !IsVideoCallCameraOn);
        EndVideoCallCommand = new RelayCommand(() => IsInVideoCall = false);

        if (_listeningService != null)
            _listeningService.VoiceLevelChanged += OnVoiceLevelChanged;
        _chatPropertyChangedHandler = (_, e) => { if (e.PropertyName == nameof(ChatViewModel.IsProcessing)) OnPropertyChanged(nameof(IsAgentProcessing)); };
        ChatVM.PropertyChanged += _chatPropertyChangedHandler;
        _themeService.ThemeChanged += OnThemeChanged;
        UpdateThemeDisplay(_themeService.CurrentTheme);
        RefreshAuthState();
        _ = CheckBackendHealthAsync();
        _ = PollEmotionalStateAsync();
        _ = AutoRefreshDashboardAsync();
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
            ThemeIcon = "🌙";
            ThemeLabel = "Modo escuro";
        }
        else
        {
            ThemeIcon = "☀️";
            ThemeLabel = "Modo claro";
        }
    }

    private async Task ToggleListeningAsync()
    {
        if (_listeningService == null) return;
        if (IsListening) { await _listeningService.StopListeningAsync().ConfigureAwait(false); IsListening = false; StatusMessage = "Escuta parada"; }
        else { await _listeningService.StartListeningAsync().ConfigureAwait(false); IsListening = true; StatusMessage = "Escutando..."; }
    }

    private async Task CheckBackendHealthAsync()
    {
        if (_kernelClient == null) return;

        if (ServiceLocator.Instance.CurrentMode == RunMode.Local)
        {
            UiThreadInvoker.Invoke(() =>
            {
                IsBackendAvailable = true;
                StatusMessage = "Modo Local";
                OnPropertyChanged(nameof(ApiEndpoint));
            });
            return;
        }

        _healthCheckCts = new CancellationTokenSource();
        var t = _healthCheckCts.Token;
        while (!t.IsCancellationRequested)
        {
            try
            {
                var isAvailable = await _kernelClient.CheckHealthAsync(t).ConfigureAwait(false);
                UiThreadInvoker.Invoke(() =>
                {
                    if (isAvailable != _previousBackendAvailable)
                    {
                        if (isAvailable)
                        {
                            StatusMessage = "Conectado";
                            KrnlLogger.Write("Backend reconectado");
                        }
                        else
                        {
                            StatusMessage = "Servidor indisponível";
                        }
                        _previousBackendAvailable = isAvailable;
                    }
                    IsBackendAvailable = isAvailable;
                    OnPropertyChanged(nameof(ApiEndpoint));
                });
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { KrnlLogger.Write($"HealthCheck: {ex.Message}"); }
            try { await Task.Delay(30000, t).ConfigureAwait(false); } catch (OperationCanceledException) { break; }
        }
    }

    private async Task RenameSessionAsync()
    {
        if (ActiveSession == null) return;
        var window = System.Windows.Application.Current.MainWindow;
        if (window == null) return;
        var dialog = new System.Windows.Window
        {
            Title = "Renomear conversa",
            Width = 400, Height = 180,
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
            Owner = window,
            WindowStyle = System.Windows.WindowStyle.ToolWindow,
            ResizeMode = System.Windows.ResizeMode.NoResize,
            ShowInTaskbar = false,
            Topmost = true
        };
        var stack = new System.Windows.Controls.StackPanel { Margin = new System.Windows.Thickness(16) };
        stack.Children.Add(new System.Windows.Controls.TextBlock { Text = "Novo nome:", Margin = new System.Windows.Thickness(0,0,0,8), FontSize = 13 });
        var inputBox = new System.Windows.Controls.TextBox { Text = ActiveSession.Title, Height = 36, FontSize = 14 };
        var confirmBtn = new System.Windows.Controls.Button { Content = "Salvar", Height = 36, Width = 100, Margin = new System.Windows.Thickness(0,12,0,0), HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        stack.Children.Add(inputBox);
        stack.Children.Add(confirmBtn);
        dialog.Content = stack;
        confirmBtn.Click += (_, _) => dialog.DialogResult = true;
        inputBox.KeyDown += (_, e) => { if (e.Key == Key.Enter) dialog.DialogResult = true; };
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(inputBox.Text) && inputBox.Text != ActiveSession.Title)
        {
            var i = Sessions.IndexOf(ActiveSession);
            Sessions[i] = new ConversationSession(ActiveSession.Id, inputBox.Text, ActiveSession.CreatedAt);
            ActiveSession = Sessions[i];
        }
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task AutoRefreshDashboardAsync()
    {
        if (_kernelClient == null) return;
        if (ServiceLocator.Instance.CurrentMode == RunMode.Local) return;

        _dashboardRefreshCts = new CancellationTokenSource();
        var t = _dashboardRefreshCts.Token;
        while (!t.IsCancellationRequested)
        {
            try { await Task.Delay(30000, t).ConfigureAwait(false); } catch (OperationCanceledException) { break; }
            if (IsBackendAvailable && !t.IsCancellationRequested)
            {
                try { await DashVM.LoadDashboardDataAsync().ConfigureAwait(false); }
                catch (Exception ex) { KrnlLogger.Write($"Dashboard refresh: {ex.Message}"); }
            }
        }
    }

    private async Task PollEmotionalStateAsync()
    {
        if (_kernelClient == null) return;
        if (ServiceLocator.Instance.CurrentMode == RunMode.Local) return;
        _emotionalPollCts = new CancellationTokenSource();
        var t = _emotionalPollCts.Token;
        while (!t.IsCancellationRequested)
        {
            try
            {
                if (IsBackendAvailable)
                {
                    var state = await _kernelClient.GetEmotionalStateAsync(UserId, t).ConfigureAwait(false);
                    if (state != null)
                    {
                        UiThreadInvoker.Invoke(() => EmotionalState = state);
                    }
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { KrnlLogger.Write($"Emotional poll: {ex.Message}"); }
            try { await Task.Delay(15000, t).ConfigureAwait(false); } catch (OperationCanceledException) { break; }
        }
    }

    private void ExecuteLogout()
    {
        if (_kernelClient == null) return;
        if (ServiceLocator.Instance.CurrentMode == RunMode.Local) return;
        var settings = _settingsService.LoadSettings();
        _settingsService.SaveSettings(settings with { AuthToken = null, RefreshToken = null, IsAuthenticated = false });
        _kernelClient.SetTokens(null, null);
        RefreshAuthState();
        LogoutRequested?.Invoke(this, EventArgs.Empty);
    }

    public void RefreshAuthState()
    {
        OnPropertyChanged(nameof(AuthenticationStatus));
        OnPropertyChanged(nameof(AuthenticationSource));
    }

    private string BuildAuthenticationStatus(AppSettings settings)
    {
        if (ServiceLocator.Instance.CurrentMode == RunMode.Local)
            return "Modo local com autenticação embutida.";

        return settings.IsAuthenticated
            ? "Sessão autenticada via JWT."
            : "Sem sessão autenticada.";
    }

    private string BuildAuthenticationSource(AppSettings settings)
    {
        if (ServiceLocator.Instance.CurrentMode == RunMode.Local)
            return "Embedded / local";

        if (!string.IsNullOrWhiteSpace(settings.AuthToken))
            return "JWT persistido";

        if (!string.IsNullOrWhiteSpace(settings.RefreshToken))
            return "Refresh token persistido";

        return "Sem token persistido";
    }

    public void StopHealthCheck()
    {
        _healthCheckCts?.Cancel();
        _healthCheckCts?.Dispose();
        _healthCheckCts = null;
        _emotionalPollCts?.Cancel();
        _emotionalPollCts?.Dispose();
        _emotionalPollCts = null;
        _dashboardRefreshCts?.Cancel();
        _dashboardRefreshCts?.Dispose();
        _dashboardRefreshCts = null;
    }

    public List<string> SearchMessages(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        return [.. ChatVM.Messages
            .Where(m => m.Content?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
            .Select(m => $"[{m.Timestamp:HH:mm}] {m.Role}: {m.Content[..Math.Min(m.Content.Length, 120)]}")];
    }

    public void Cleanup()
    {
        StopHealthCheck();
        if (_listeningService != null)
            _listeningService.VoiceLevelChanged -= OnVoiceLevelChanged;
        _themeService.ThemeChanged -= OnThemeChanged;
        if (_chatPropertyChangedHandler != null)
            ChatVM.PropertyChanged -= _chatPropertyChangedHandler;
        ChatVM.Cleanup();
        (DashVM as IDisposable)?.Dispose();
        (SettingsVM as IDisposable)?.Dispose();
    }

    private void OnVoiceLevelChanged(object? sender, float level) => UiThreadInvoker.Invoke(() => VoiceLevel = level);
}
