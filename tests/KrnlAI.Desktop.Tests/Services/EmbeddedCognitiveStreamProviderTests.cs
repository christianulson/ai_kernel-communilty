using KrnlAI.Core.Abstractions;
using KrnlAI.Core.Model;
using KrnlAI.Core.Services;
using KrnlAI.Desktop.App.Services;
using CoreCognitiveCycleEvent = KrnlAI.Core.Model.CognitiveCycleEvent;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class EmbeddedCognitiveStreamProviderTests
{
    [Fact]
    public async Task ConnectAsync_ShouldInvokeOnStateChanged()
    {
        var stateChanges = new List<KrnlAI.Desktop.Core.Abstractions.CognitiveStreamState>();
        var streamer = new TestCognitiveStreamer();
        var provider = new EmbeddedCognitiveStreamProvider(streamer);
        provider.OnStateChanged += s => stateChanges.Add(s);

        await provider.ConnectAsync(ct: CancellationToken.None);

        Assert.Contains(KrnlAI.Desktop.Core.Abstractions.CognitiveStreamState.Connected, stateChanges);
    }

    [Fact]
    public void Disconnect_ShouldInvokeDisconnected()
    {
        var stateChanges = new List<KrnlAI.Desktop.Core.Abstractions.CognitiveStreamState>();
        var streamer = new TestCognitiveStreamer();
        var provider = new EmbeddedCognitiveStreamProvider(streamer);
        provider.OnStateChanged += s => stateChanges.Add(s);

        provider.Disconnect();

        Assert.Contains(KrnlAI.Desktop.Core.Abstractions.CognitiveStreamState.Disconnected, stateChanges);
    }

    [Fact]
    public async Task ConnectAsync_ShouldReceiveEvents()
    {
        var receivedEvents = new List<KrnlAI.Desktop.Core.Abstractions.CognitiveCycleEvent>();
        var streamer = new TestCognitiveStreamer();
        var provider = new EmbeddedCognitiveStreamProvider(streamer);
        provider.OnEvent += e => receivedEvents.Add(e);

        await provider.ConnectAsync(ct: CancellationToken.None);

        streamer.EmitTestEvent(new CoreCognitiveCycleEvent(
            CognitiveEventType.StepStarted, "test-step", "content", null,
            DateTimeOffset.UtcNow, "cycle-1", null));

        Assert.Single(receivedEvents);
        Assert.Equal("test-step", receivedEvents[0].StepName);
    }

    private sealed class TestCognitiveStreamer : ICognitiveStreamer
    {
        private readonly List<ICognitiveStreamSink> _sinks = new();

        public Task EmitAsync(CoreCognitiveCycleEvent evt, CancellationToken ct = default) => Task.CompletedTask;
        public Task EmitBatchAsync(IReadOnlyList<CoreCognitiveCycleEvent> events, CancellationToken ct = default) => Task.CompletedTask;

        public IDisposable Subscribe(ICognitiveStreamSink sink)
        {
            _sinks.Add(sink);
            return new Subscription(() => _sinks.Remove(sink));
        }

        public void EmitTestEvent(CoreCognitiveCycleEvent evt)
        {
            foreach (var sink in _sinks)
                sink.OnEventAsync(evt, CancellationToken.None);
        }

        private sealed class Subscription(Action onDispose) : IDisposable
        {
            public void Dispose() => onDispose();
        }
    }
}
