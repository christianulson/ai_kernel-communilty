using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Services;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public class VideoCallViewModel : ViewModelBase, IDisposable
{
    private readonly ILogger<VideoCallViewModel> _logger;
    private readonly IWebRtcService? _webRtc;

    private bool _isInCall;
    private string _state = "Idle";
    private bool _muted, _cameraOn = true;
    private string _remotePeerId = "";
    private bool _disposed;

    public bool IsInVideoCall { get => _isInCall; set => SetProperty(ref _isInCall, value); }
    public string VideoCallState { get => _state; set { SetProperty(ref _state, value); OnPropertyChanged(nameof(VideoCallStateText)); } }
    public string VideoCallStateText => _state switch
    {
        "Connecting" => "Conectando...", "Ringing" => "Chamando...", "Connected" => "Em chamada",
        "Ended" => "Encerrada", "Failed" => "Falhou", _ => "Pronto"
    };
    public bool IsVideoCallMuted { get => _muted; set => SetProperty(ref _muted, value); }
    public bool IsVideoCallCameraOn { get => _cameraOn; set => SetProperty(ref _cameraOn, value); }
    public string RemotePeerId { get => _remotePeerId; set => SetProperty(ref _remotePeerId, value); }

    public ICommand ToggleCallCommand { get; }
    public ICommand EndCallCommand { get; }
    public ICommand ToggleMuteCommand { get; }
    public ICommand ToggleCameraCommand { get; }

    public VideoCallViewModel(IWebRtcService webRtc)
    {
        _logger = ServiceLocator.Instance.GetLogger<VideoCallViewModel>();
        _webRtc = webRtc;

        ToggleCallCommand = new AsyncRelayCommand(ToggleCallAsync);
        EndCallCommand = new AsyncRelayCommand(EndCallAsync);
        ToggleMuteCommand = new RelayCommand(() => IsVideoCallMuted = !IsVideoCallMuted);
        ToggleCameraCommand = new RelayCommand(() => IsVideoCallCameraOn = !IsVideoCallCameraOn);

        if (_webRtc != null)
            _webRtc.StateChanged += OnWebRtcStateChanged;
    }

    public VideoCallViewModel() : this(ServiceLocator.Instance.WebRtcServiceFactory()) { }

    private async Task ToggleCallAsync()
    {
        if (IsInVideoCall) { await EndCallAsync().ConfigureAwait(false); return; }
        if (_webRtc == null) { VideoCallState = "Failed"; return; }

        IsVideoCallMuted = false;
        IsVideoCallCameraOn = true;
        VideoCallState = "Connecting";

        var settings = ServiceLocator.Instance.SettingsService.LoadSettings();
        var baseUrl = settings.ApiEndpoint ?? settings.ApiBaseUrl ?? "http://localhost:5235";
        var signalingUrl = baseUrl.TrimEnd('/') + "/signaling/webrtc";

        var initialized = await _webRtc.InitializeAsync(signalingUrl, "stun.l.google.com:19302").ConfigureAwait(false);
        if (!initialized) { VideoCallState = "Failed"; return; }

        if (!string.IsNullOrWhiteSpace(RemotePeerId))
        {
            var connected = await _webRtc.ConnectToPeerAsync(RemotePeerId).ConfigureAwait(false);
            if (!connected) VideoCallState = "Failed";
        }
    }

    private async Task EndCallAsync()
    {
        if (_webRtc == null) return;
        await _webRtc.DisconnectAsync().ConfigureAwait(false);
        IsInVideoCall = false;
        VideoCallState = "Ended";
        await Task.Delay(1500).ConfigureAwait(false);
        VideoCallState = "Idle";
    }

    public void Cleanup()
    {
        if (!_disposed) Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_webRtc != null)
            _webRtc.StateChanged -= OnWebRtcStateChanged;
    }

    private void OnWebRtcStateChanged(object? sender, WebRtcEventArgs e)
    {
        switch (e.State)
        {
            case WebRtcState.Connecting:
                VideoCallState = "Connecting";
                break;
            case WebRtcState.Connected:
                IsInVideoCall = true;
                VideoCallState = "Connected";
                if (!string.IsNullOrEmpty(e.PeerId)) RemotePeerId = e.PeerId;
                _logger.LogInformation("VideoCall connected to {PeerId}", e.PeerId);
                break;
            case WebRtcState.Failed:
                VideoCallState = "Failed";
                _logger.LogError("VideoCall failed: {Message}", e.Message);
                break;
            case WebRtcState.Closed:
                IsInVideoCall = false;
                VideoCallState = "Idle";
                break;
        }
    }
}
