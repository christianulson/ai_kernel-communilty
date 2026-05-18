using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Services;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public class VideoCallViewModel : ViewModelBase
{
    private readonly ServiceLocator _services;
    private readonly ILogger<VideoCallViewModel> _logger;
    private readonly Func<WebRtcService> _webRtcFactory;
    private readonly IWebRtcService _webRtc;

    private bool _isInCall;
    private string _state = "Idle";
    private bool _muted, _cameraOn = true;
    private string _remotePeerId = "";

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

    public VideoCallViewModel()
    {
        _services = ServiceLocator.Instance;
        _logger = ServiceLocator.Instance.GetLogger<VideoCallViewModel>();
        _webRtcFactory = _services.WebRtcServiceFactory;
        _webRtc = _webRtcFactory();

        ToggleCallCommand = new AsyncRelayCommand(ToggleCallAsync);
        EndCallCommand = new AsyncRelayCommand(EndCallAsync);
        ToggleMuteCommand = new RelayCommand(() => IsVideoCallMuted = !IsVideoCallMuted);
        ToggleCameraCommand = new RelayCommand(() => IsVideoCallCameraOn = !IsVideoCallCameraOn);

        _webRtc.StateChanged += OnWebRtcStateChanged;
    }

    private async Task ToggleCallAsync()
    {
        if (IsInVideoCall) { await EndCallAsync(); return; }

        IsVideoCallMuted = false;
        IsVideoCallCameraOn = true;
        VideoCallState = "Connecting";

        var settings = _services.SettingsService.LoadSettings();
        var signalingUrl = (settings.ApiBaseUrl ?? "http://localhost:5000").TrimEnd('/') + "/signaling/webrtc";

        var initialized = await _webRtc.InitializeAsync(signalingUrl, "stun.l.google.com:19302");
        if (!initialized) { VideoCallState = "Failed"; return; }

        if (!string.IsNullOrWhiteSpace(RemotePeerId))
        {
            var connected = await _webRtc.ConnectToPeerAsync(RemotePeerId);
            if (!connected) VideoCallState = "Failed";
        }
    }

    private async Task EndCallAsync()
    {
        await _webRtc.DisconnectAsync();
        IsInVideoCall = false;
        VideoCallState = "Ended";
        await Task.Delay(1500);
        VideoCallState = "Idle";
    }

    public void Cleanup()
    {
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
