namespace KrnlAI.Desktop.Core.Models;

public record MediaDevice(
    string Id,
    string Name,
    MediaDeviceType Type
);

public enum MediaDeviceType
{
    AudioInput,
    AudioOutput,
    VideoInput
}

public record AppSettings
{
    public string? SelectedMicrophoneId { get; init; }
    public string? SelectedCameraId { get; init; }
    public string? SelectedSpeakerId { get; init; }
    public bool ContinuousListeningEnabled { get; init; }
    public string? GlobalHotkey { get; init; }
    public float VoiceDetectionThreshold { get; init; } = 0.01f;
    public int SilenceDurationMs { get; init; } = 1500;
    public string ApiBaseUrl { get; init; } = "http://localhost:5235";
    public float SpeakerVolume { get; init; } = 1.0f;
    public string? ApiEndpoint { get; init; } = "http://localhost:5235";
    public string? AuthToken { get; init; }
    public string? RefreshToken { get; init; }
    public string? Username { get; init; }
    public DateTime? TokenExpiresAt { get; init; }
    public bool IsAuthenticated { get; init; }
    
    public double WindowLeft { get; init; } = double.NaN;
    public double WindowTop { get; init; } = double.NaN;
    public double WindowWidth { get; init; } = 1200;
    public double WindowHeight { get; init; } = 800;
    public bool WindowMaximized { get; init; }
    
    public string Theme { get; init; } = "dark";
    public bool? AutoDarkMode { get; init; }
    public int ChatFontSize { get; init; } = 14;
    public bool NotifyOnComplete { get; init; } = true;
    public string? Language { get; init; }
    public SidecarSettings? SidecarConfig { get; init; }
}

public record SidecarSettings(
    string? Mode,
    string? AuthEndpoint,
    string? GatewayEndpoint,
    string? ApiKey,
    string? TenantId
);
