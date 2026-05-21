namespace KrnlAI.VisualStudio.Services;

public interface ISignalRStreamingService
{
    ConnectionState State { get; }
    event Action<string>? TokenReceived;
    event Action<string>? ArtifactReceived;
    event Action<string>? ErrorReceived;
    event Action? StreamCompleted;
    event Action<ConnectionState>? StateChanged;

    Task ConnectAsync(string hubUrl, CancellationToken ct = default);
    Task DisconnectAsync();
    Task StartAgentStreamAsync(string goal, string sessionId, CancellationToken ct = default);
}
