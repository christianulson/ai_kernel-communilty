using KrnlAI.Core.Abstractions;
using CoreModel = KrnlAI.Core.Model;
using CoreAbstractions = KrnlAI.Desktop.Core.Abstractions;

namespace KrnlAI.Desktop.App.Services;

public sealed class EmbeddedCognitiveStreamProvider(ICognitiveStreamer streamer) : CoreAbstractions.ICognitiveStreamProvider
{
    private IDisposable? _subscription;
    private string? _activeCycleId;

    public CoreAbstractions.CognitiveStreamState State { get; private set; } = CoreAbstractions.CognitiveStreamState.Disconnected;
    public event Action<CoreAbstractions.CognitiveCycleEvent>? OnEvent;
    public event Action<CoreAbstractions.CognitiveStreamState>? OnStateChanged;

    public Task ConnectAsync(string? cycleId = null, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return Task.FromCanceled(ct);

        _activeCycleId = cycleId;
        State = CoreAbstractions.CognitiveStreamState.Connecting;
        OnStateChanged?.Invoke(State);

        try
        {
            _subscription = streamer.Subscribe(new EmbeddedStreamSink(this));
            State = CoreAbstractions.CognitiveStreamState.Connected;
            OnStateChanged?.Invoke(State);
        }
        catch (Exception ex)
        {
            Core.Services.KrnlLogger.Write($"EmbeddedCognitiveStreamProvider: Subscribe failed: {ex.Message}");
            State = CoreAbstractions.CognitiveStreamState.Error;
            OnStateChanged?.Invoke(State);
        }

        return Task.CompletedTask;
    }

    public void Disconnect()
    {
        _subscription?.Dispose();
        _subscription = null;
        _activeCycleId = null;
        State = CoreAbstractions.CognitiveStreamState.Disconnected;
        OnStateChanged?.Invoke(State);
    }

    private sealed class EmbeddedStreamSink(EmbeddedCognitiveStreamProvider owner) : ICognitiveStreamSink
    {
        public Task OnEventAsync(CoreModel.CognitiveCycleEvent evt, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                return Task.FromCanceled(ct);

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
