using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

public interface IAudioPlayback : IDisposable
{
    event EventHandler? PlaybackStarted;
    event EventHandler? PlaybackStopped;
    Task PlayAsync(byte[] audioData, CancellationToken cancellationToken = default);
    void Stop();
    bool IsPlaying { get; }
    IReadOnlyList<MediaDevice> GetAvailableDevices();
    void SetDevice(string? deviceId);
    void SetVolume(float volume);
}