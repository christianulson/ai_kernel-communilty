namespace KrnlAI.Desktop.Core.Abstractions;

public interface IListeningService : IDisposable
{
    event EventHandler<float>? VoiceLevelChanged;
    event EventHandler<ListeningEventArgs>? SpeechDetected;
    event EventHandler<string>? ResponseReceived;
    Task StartListeningAsync(CancellationToken cancellationToken = default);
    Task StopListeningAsync();
    bool IsListening { get; }
    void SetThreshold(float threshold);
    void SetSilenceDuration(int milliseconds);
}

public record ListeningEventArgs(
    byte[] AudioData,
    TimeSpan Duration
);