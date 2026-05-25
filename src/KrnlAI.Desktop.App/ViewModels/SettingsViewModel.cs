using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Desktop.Core.Services;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public class SettingsViewModel : ViewModelBase, IDisposable
{
    private readonly IKernelClient? _kernelClient;
    private readonly ISettingsService _settingsService;
    private readonly IListeningService? _listeningService;
    private readonly IAudioPlayback _audioPlayback;
    private readonly IThemeService _themeService;
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly System.Threading.Timer? _debounceTimer;
    private const int DebounceMs = 500;
    private string _apiEndpoint = "http://localhost:5235";
    private bool _disposed;
    public string ApiEndpoint
    {
        get => _apiEndpoint;
        set
        {
            if (string.IsNullOrEmpty(value) || (!value.StartsWith("http://") && !value.StartsWith("https://")))
            {
                _logger.LogWarning("Invalid API endpoint rejected: {Value}", value);
                return;
            }
            if (SetProperty(ref _apiEndpoint, value))
            {
                if (_kernelClient != null) _kernelClient.SetBaseUrl(value);
                _debounceTimer?.Change(DebounceMs, System.Threading.Timeout.Infinite);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _themeService.ThemeChanged -= OnExternalThemeChanged;
        _debounceTimer?.Dispose();
    }
    private MediaDevice? _selectedMic, _selectedCam, _selectedSpeaker;
    public MediaDevice? SelectedMicrophone { get => _selectedMic; set { if (SetProperty(ref _selectedMic, value)) Save(); } }
    public MediaDevice? SelectedCamera { get => _selectedCam; set { if (SetProperty(ref _selectedCam, value)) Save(); } }
    public MediaDevice? SelectedSpeaker { get => _selectedSpeaker; set { if (SetProperty(ref _selectedSpeaker, value)) { _audioPlayback.SetDevice(value?.Id); Save(); } } }
    public ObservableCollection<MediaDevice> Microphones { get; } = new();
    public ObservableCollection<MediaDevice> Cameras { get; } = new();
    public ObservableCollection<MediaDevice> Speakers { get; } = new();
    private float _speakerVol = 1.0f;
    public float SpeakerVolume { get => _speakerVol; set { if (SetProperty(ref _speakerVol, value)) { _audioPlayback.SetVolume(value); Save(); } } }
    private float _vadThreshold = 0.01f;
    public float VadThreshold { get => _vadThreshold; set { if (SetProperty(ref _vadThreshold, value)) { _listeningService?.SetThreshold(value); Save(); } } }
    private int _silenceMs = 1500;
    public int SilenceDurationMs { get => _silenceMs; set { if (SetProperty(ref _silenceMs, value)) { _listeningService?.SetSilenceDuration(value); Save(); } } }
    private bool _darkTheme = true, _lightTheme;
    public bool IsDarkTheme { get => _darkTheme; set { if (_syncingTheme || !value) return; _darkTheme = true; _lightTheme = false; ApplyTheme("dark"); OnPropertyChanged(nameof(IsDarkTheme)); OnPropertyChanged(nameof(IsLightTheme)); } }
    public bool IsLightTheme { get => _lightTheme; set { if (_syncingTheme || !value) return; _lightTheme = true; _darkTheme = false; ApplyTheme("light"); OnPropertyChanged(nameof(IsDarkTheme)); OnPropertyChanged(nameof(IsLightTheme)); } }

    // Language
    public List<string> AvailableLanguages { get; } = new() { "pt-BR", "en" };
    private string _selectedLanguage = "pt-BR";
    public string SelectedLanguage { get => _selectedLanguage; set { if (SetProperty(ref _selectedLanguage, value)) { ServiceLocator.Instance.LocalizationService.SetCulture(value); Save(); } } }
    private string _languageLabel = "Português (Brasil)";
    public string LanguageLabel { get => _languageLabel; set => SetProperty(ref _languageLabel, value); }
    private bool _minTray = true, _notifyMsg = true, _notifyCall = true, _notifySys = true, _notifySound = true;
    public bool MinimizeToTrayOnClose { get => _minTray; set => SetProperty(ref _minTray, value); }
    public bool NotifyMessages { get => _notifyMsg; set => SetProperty(ref _notifyMsg, value); }
    public bool NotifyCalls { get => _notifyCall; set => SetProperty(ref _notifyCall, value); }
    public bool NotifySystem { get => _notifySys; set => SetProperty(ref _notifySys, value); }
    public bool NotificationSound { get => _notifySound; set => SetProperty(ref _notifySound, value); }
    private string _listeningHk = "Ctrl+Shift+K";
    public string ListeningHotkey { get => _listeningHk; set => SetProperty(ref _listeningHk, value); }
    private string _stun = "stun:stun.l.google.com:19302", _turn = "";
    public string StunServer { get => _stun; set => SetProperty(ref _stun, value); }
    public string TurnServer { get => _turn; set => SetProperty(ref _turn, value); }
    private string _deviceStatus = "";
    public string DeviceTestStatus { get => _deviceStatus; set => SetProperty(ref _deviceStatus, value); }
    public ICommand TestSpeakerCommand { get; }
    public ICommand TestMicrophoneCommand { get; }
    public ICommand TestCameraCommand { get; }
    public ICommand TestWebRtcCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand SaveHotkeysCommand { get; }

    public SettingsViewModel(IKernelClient kernelClient, ISettingsService settingsService, IListeningService listeningService, IAudioPlayback audioPlayback, IThemeService themeService)
    {
        _kernelClient = kernelClient;
        _settingsService = settingsService;
        _listeningService = listeningService;
        _audioPlayback = audioPlayback;
        _themeService = themeService;
        _themeService.ThemeChanged += OnExternalThemeChanged;
        _logger = ServiceLocator.Instance.GetLogger<SettingsViewModel>();
        _debounceTimer = new System.Threading.Timer(_ => { System.Windows.Application.Current?.Dispatcher.Invoke(() => Save()); }, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        var s = _settingsService.LoadSettings();
        _apiEndpoint = s.ApiEndpoint ?? s.ApiBaseUrl;
        _kernelClient?.SetBaseUrl(_apiEndpoint);
        SyncThemeFromService();
        _listeningService?.SetThreshold(s.VoiceDetectionThreshold);
        _listeningService?.SetSilenceDuration(s.SilenceDurationMs);
        if (!string.IsNullOrEmpty(s.SelectedSpeakerId)) _audioPlayback.SetDevice(s.SelectedSpeakerId);
        _audioPlayback.SetVolume(s.SpeakerVolume);

        TestSpeakerCommand = new AsyncRelayCommand(TestSpeakerAsync);
        TestMicrophoneCommand = new AsyncRelayCommand(TestMicAsync);
        TestCameraCommand = new AsyncRelayCommand(TestCamAsync);
        TestWebRtcCommand = new AsyncRelayCommand(TestRtcAsync);
        ToggleThemeCommand = new RelayCommand(() => { if (IsDarkTheme) { IsDarkTheme = false; IsLightTheme = true; } else { IsDarkTheme = true; IsLightTheme = false; } });
        SaveHotkeysCommand = new RelayCommand(() => { var ss = _settingsService.LoadSettings(); _settingsService.SaveSettings(ss with { GlobalHotkey = ListeningHotkey }); });

        try { LoadDevices(); } catch (Exception ex) { _logger.LogError(ex, "Failed to load devices"); }
        try { _ = LoadMcpServersAsync(); } catch (Exception ex) { _logger.LogError(ex, "Failed to load MCP"); }
    }

    public SettingsViewModel() : this(
        ServiceLocator.Instance.KernelClient,
        ServiceLocator.Instance.SettingsService,
        ServiceLocator.Instance.ListeningService,
        ServiceLocator.Instance.AudioPlayback,
        ServiceLocator.Instance.ThemeSvc) { }

    private void OnExternalThemeChanged(object? sender, string themeName)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() => SyncThemeFromService());
    }

    private bool _syncingTheme;

    private void SyncThemeFromService()
    {
        if (_syncingTheme) return;
        _syncingTheme = true;
        try
        {
            var current = _themeService.CurrentTheme;
            var isDark = string.Equals(current, "dark", StringComparison.OrdinalIgnoreCase);
            if (isDark != _darkTheme)
            {
                _darkTheme = isDark;
                _lightTheme = !isDark;
                OnPropertyChanged(nameof(IsDarkTheme));
                OnPropertyChanged(nameof(IsLightTheme));
            }
        }
        finally
        {
            _syncingTheme = false;
        }
    }

    public void LoadDevices()
    {
        Microphones.Clear(); Speakers.Clear(); Cameras.Clear();
        try { foreach (var d in ServiceLocator.Instance.AudioCapture.GetAvailableDevices()) Microphones.Add(d); } catch (Exception ex) { _logger.LogError(ex, "LoadDevices (Microphones)"); }
        try { foreach (var d in _audioPlayback.GetAvailableDevices()) Speakers.Add(d); } catch (Exception ex) { _logger.LogError(ex, "LoadDevices (Speakers)"); }
        try { foreach (var d in ServiceLocator.Instance.VideoCapture.GetAvailableDevices()) Cameras.Add(d); } catch (Exception ex) { _logger.LogError(ex, "LoadDevices (Cameras)"); }
        if (Microphones.Count > 0) SelectedMicrophone = Microphones[0];
        if (Speakers.Count > 0) SelectedSpeaker = Speakers[0];
        if (Cameras.Count > 0) SelectedCamera = Cameras[0];
    }

    private void Save()
    {
        var s = _settingsService.LoadSettings();
        _settingsService.SaveSettings(s with
        {
            SelectedMicrophoneId = SelectedMicrophone?.Id, SelectedCameraId = SelectedCamera?.Id,
            SelectedSpeakerId = SelectedSpeaker?.Id, ApiBaseUrl = _apiEndpoint, ApiEndpoint = _apiEndpoint,
            SpeakerVolume = _speakerVol, VoiceDetectionThreshold = _vadThreshold, SilenceDurationMs = _silenceMs,
            Theme = IsDarkTheme ? "dark" : "light", AuthToken = s.AuthToken, Username = s.Username,
            IsAuthenticated = s.IsAuthenticated
        });
    }

    private void ApplyTheme(string t) { _themeService.SetTheme(t); }
    private async Task TestSpeakerAsync()
    {
        if (ServiceLocator.Instance.CurrentMode == RunMode.Local)
        {
            DeviceTestStatus = "Indisponível no modo Local";
            await Task.Delay(1500);
            DeviceTestStatus = "";
            return;
        }
        DeviceTestStatus = "Testando...";
        var a = _kernelClient != null ? await _kernelClient.GenerateSpeechAsync("Teste") : [];
        if (a.Length > 0) await _audioPlayback.PlayAsync(a);
        DeviceTestStatus = "OK";
        await Task.Delay(1500);
        DeviceTestStatus = "";
    }
    private async Task TestMicAsync()
    {
        if (ServiceLocator.Instance.CurrentMode == RunMode.Local)
        {
            DeviceTestStatus = "Teste local (sem transcrição)";
            await ServiceLocator.Instance.AudioCapture.StartCaptureAsync(SelectedMicrophone?.Id);
            await Task.Delay(2000);
            await ServiceLocator.Instance.AudioCapture.StopCaptureAsync();
            DeviceTestStatus = "OK";
            await Task.Delay(1500);
            DeviceTestStatus = "";
            return;
        }
        DeviceTestStatus = "Gravando...";
        await ServiceLocator.Instance.AudioCapture.StartCaptureAsync(SelectedMicrophone?.Id);
        await Task.Delay(2000);
        await ServiceLocator.Instance.AudioCapture.StopCaptureAsync();
        DeviceTestStatus = "OK";
        await Task.Delay(1500);
        DeviceTestStatus = "";
    }
    private async Task TestCamAsync() { DeviceTestStatus = "Testando..."; try { var c = ServiceLocator.Instance.VideoCapture.GetAvailableDevices(); DeviceTestStatus = c.Any() ? $"Câmera: {c[0].Name}" : "Nenhuma"; } catch { DeviceTestStatus = "Erro"; } await Task.Delay(1500); DeviceTestStatus = ""; }
    private async Task TestRtcAsync() { DeviceTestStatus = "Não implementado (WebRTC)"; await Task.Delay(1500); DeviceTestStatus = ""; }

    // --- MCP Servers ---
    public ObservableCollection<McpServerInfo> McpServers { get; } = new();

    public async Task LoadMcpServersAsync()
    {
        if (_kernelClient == null) return;
        if (ServiceLocator.Instance.CurrentMode == RunMode.Local) return;
        var servers = await _kernelClient.GetMcpServersAsync();
        UiThreadInvoker.Invoke(() =>
        {
            McpServers.Clear();
            foreach (var s in servers) McpServers.Add(s);
        });
    }

    public async Task ToggleMcpServerAsync(string serverId, bool enabled)
    {
        if (_kernelClient == null) return;
        if (ServiceLocator.Instance.CurrentMode == RunMode.Local) return;
        var ok = await _kernelClient.ToggleMcpServerAsync(serverId, enabled);
        if (ok) await LoadMcpServersAsync();
    }
}
