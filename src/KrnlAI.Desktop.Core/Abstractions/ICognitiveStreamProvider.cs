namespace KrnlAI.Desktop.Core.Abstractions;

public enum CognitiveStreamState { Disconnected, Connecting, Connected, Error }

public sealed record CognitiveCycleEvent(
    string Type,
    string StepName,
    string? Content,
    string CycleId,
    string Timestamp);

public interface ICognitiveStreamProvider
{
    event Action<CognitiveCycleEvent>? OnEvent;
    event Action<CognitiveStreamState>? OnStateChanged;
    Task ConnectAsync(string? cycleId = null, CancellationToken ct = default);
    void Disconnect();
}
