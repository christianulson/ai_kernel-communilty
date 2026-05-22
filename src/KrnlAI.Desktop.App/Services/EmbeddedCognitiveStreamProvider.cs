using KrnlAI.Core.Abstractions;
using CoreModel = KrnlAI.Core.Model;
using CoreAbstractions = KrnlAI.Desktop.Core.Abstractions;

namespace KrnlAI.Desktop.App.Services;

public sealed class EmbeddedCognitiveStreamProvider : CoreAbstractions.ICognitiveStreamProvider
{
    private readonly ICognitiveStreamer _streamer;
    private IDisposable? _subscription;

    public CoreAbstractions.CognitiveStreamState State { get; private set; } = CoreAbstractions.CognitiveStreamState.Disconnected;
    public event Action<CoreAbstractions.CognitiveCycleEvent>? OnEvent;
    public event Action<CoreAbstractions.CognitiveStreamState>? OnStateChanged;

    public EmbeddedCognitiveStreamProvider(ICognitiveStreamer streamer)
    {
        _streamer = streamer;
    }

    public Task ConnectAsync(string? cycleId = null, CancellationToken ct = default)
    {
        State = CoreAbstractions.CognitiveStreamState.Connecting;
        OnStateChanged?.Invoke(State);

        State = CoreAbstractions.CognitiveStreamState.Connected;
        OnStateChanged?.Invoke(State);

        _subscription = _streamer.Subscribe(new EmbeddedStreamSink(this));
        return Task.CompletedTask;
    }

    public void Disconnect()
    {
        _subscription?.Dispose();
        _subscription = null;
        State = CoreAbstractions.CognitiveStreamState.Disconnected;
        OnStateChanged?.Invoke(State);
    }

    private sealed class EmbeddedStreamSink(EmbeddedCognitiveStreamProvider owner) : ICognitiveStreamSink
    {
        public Task OnEventAsync(CoreModel.CognitiveCycleEvent evt, CancellationToken ct)
        {
            var desktopEvent = new CoreAbstractions.CognitiveCycleEvent(
                evt.Type.ToString(),
                evt.StepName,
                evt.Content,
                evt.CycleId,
                evt.Timestamp.ToString("O"));
            owner.OnEvent?.Invoke(desktopEvent);
            return Task.CompletedTask;
        }
    }
}
