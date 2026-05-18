using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

public interface IAudioCapture : IDisposable
{
    event EventHandler<float>? VoiceLevelChanged;
    Task StartCaptureAsync(string? deviceId = null);
    Task<byte[]> StopCaptureAndGetAudioAsync();
    Task StopCaptureAsync();
    bool IsCapturing { get; }
    IReadOnlyList<MediaDevice> GetAvailableDevices();
}

public record AudioCaptureEventArgs(
    byte[] AudioData,
    int SampleRate,
    int Channels,
    int BitsPerSample,
    TimeSpan Duration
);