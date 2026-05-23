using KrnlAI.Core.Abstractions;
using KrnlAI.Core.Model;
using KrnlAI.Core.Services;
using KrnlAI.Desktop.App.Services;
using CoreAbstractions = KrnlAI.Desktop.Core.Abstractions;
using CoreCognitiveCycleEvent = KrnlAI.Core.Model.CognitiveCycleEvent;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class EmbeddedCognitiveStreamProviderTests
{
    [Fact]
    public async Task ConnectAsync_ShouldInvokeOnStateChanged()
    {
        var stateChanges = new List<CoreAbstractions.CognitiveStreamState>();
        var streamer = new TestCognitiveStreamer();
        var provider = new EmbeddedCognitiveStreamProvider(streamer);
        provider.OnStateChanged += s => stateChanges.Add(s);

        await provider.ConnectAsync(ct: CancellationToken.None);

        Assert.Contains(CoreAbstractions.CognitiveStreamState.Connected, stateChanges);
    }

    [Fact]
    public void Disconnect_ShouldInvokeDisconnected()
    {
        var stateChanges = new List<CoreAbstractions.CognitiveStreamState>();
        var streamer = new TestCognitiveStreamer();
        var provider = new EmbeddedCognitiveStreamProvider(streamer);
        provider.OnStateChanged += s => stateChanges.Add(s);

        provider.Disconnect();

        Assert.Contains(CoreAbstractions.CognitiveStreamState.Disconnected, stateChanges);
    }

    [Fact]
    public async Task ConnectAsync_ShouldReceiveEvents()
    {
        var receivedEvents = new List<CoreAbstractions.CognitiveCycleEvent>();
        var streamer = new TestCognitiveStreamer();
        var provider = new EmbeddedCognitiveStreamProvider(streamer);
        provider.OnEvent += e => receivedEvents.Add(e);

        await provider.ConnectAsync(ct: CancellationToken.None);
        streamer.EmitTestEvent(new CoreCognitiveCycleEvent(
            CognitiveEventType.StepStarted, "test-step", "content", null,
            DateTimeOffset.UtcNow, "cycle-1", null));

        Assert.Single(receivedEvents);
        Assert.Equal("test-step", receivedEvents[0].StepName);
        Assert.Equal("StepStarted", receivedEvents[0].Type);
    }

    [Fact]
    public async Task ConnectAsync_MultipleEvents_ShouldReceiveAll()
    {
        var receivedEvents = new List<CoreAbstractions.CognitiveCycleEvent>();
        var streamer = new TestCognitiveStreamer();
        var provider = new EmbeddedCognitiveStreamProvider(streamer);
        provider.OnEvent += e => receivedEvents.Add(e);

        await provider.ConnectAsync(ct: CancellationToken.None);

        streamer.EmitTestEvent(new CoreCognitiveCycleEvent(CognitiveEventType.StepStarted, "s1", "c1", null, DateTimeOffset.UtcNow, "c1", null));
        streamer.EmitTestEvent(new CoreCognitiveCycleEvent(CognitiveEventType.StepCompleted, "s2", "c2", null, DateTimeOffset.UtcNow, "c1", null));
        streamer.EmitTestEvent(new CoreCognitiveCycleEvent(CognitiveEventType.CycleCompleted, "s3", "c3", null, DateTimeOffset.UtcNow, "c1", null));

        Assert.Equal(3, receivedEvents.Count);
        Assert.Equal("s1", receivedEvents[0].StepName);
        Assert.Equal("s2", receivedEvents[1].StepName);
        Assert.Equal("s3", receivedEvents[2].StepName);
    }

    [Fact]
    public async Task ConnectAsync_Disconnect_ShouldStopReceivingEvents()
    {
        var receivedEvents = new List<CoreAbstractions.CognitiveCycleEvent>();
        var streamer = new TestCognitiveStreamer();
        var provider = new EmbeddedCognitiveStreamProvider(streamer);
        provider.OnEvent += e => receivedEvents.Add(e);

        await provider.ConnectAsync(ct: CancellationToken.None);
        provider.Disconnect();

        streamer.EmitTestEvent(new CoreCognitiveCycleEvent(CognitiveEventType.StepStarted, "after-disc", "", null, DateTimeOffset.UtcNow, "c1", null));

        Assert.Empty(receivedEvents);
    }

    [Fact]
    public async Task Disconnect_WithoutConnect_ShouldNotThrow()
    {
        var streamer = new TestCognitiveStreamer();
        var provider = new EmbeddedCognitiveStreamProvider(streamer);
        provider.Disconnect();
    }

    [Fact]
    public async Task ConnectAsync_Disconnect_Reconnect_ShouldWork()
    {
        var stateChanges = new List<CoreAbstractions.CognitiveStreamState>();
        var streamer = new TestCognitiveStreamer();
        var provider = new EmbeddedCognitiveStreamProvider(streamer);
        provider.OnStateChanged += s => stateChanges.Add(s);

        await provider.ConnectAsync(ct: CancellationToken.None);
        provider.Disconnect();
        await provider.ConnectAsync(ct: CancellationToken.None);

        Assert.Equal(CoreAbstractions.CognitiveStreamState.Connected, provider.State);
    }

    [Fact]
    public async Task AllEventTypes_ShouldBeMappedCorrectly()
    {
        var received = new List<CoreAbstractions.CognitiveCycleEvent>();
        var streamer = new TestCognitiveStreamer();
        var provider = new EmbeddedCognitiveStreamProvider(streamer);
        provider.OnEvent += e => received.Add(e);

        await provider.ConnectAsync(ct: CancellationToken.None);

        var types = new[] {
            (CognitiveEventType.StepStarted, "StepStarted"),
            (CognitiveEventType.StepCompleted, "StepCompleted"),
            (CognitiveEventType.ToolCalled, "ToolCalled"),
            (CognitiveEventType.Error, "Error"),
            (CognitiveEventType.CycleCompleted, "CycleCompleted"),
            (CognitiveEventType.Thought, "Thought"),
        };
        foreach (var (evtType, _) in types)
        {
            streamer.EmitTestEvent(new CoreCognitiveCycleEvent(evtType, "step", "", null, DateTimeOffset.UtcNow, "c1", null));
        }

        Assert.Equal(types.Length, received.Count);
        for (var i = 0; i < types.Length; i++)
            Assert.Equal(types[i].Item1.ToString(), received[i].Type);
    }

    private sealed class TestCognitiveStreamer : ICognitiveStreamer
    {
        private readonly List<ICognitiveStreamSink> _sinks = new();
        public Task EmitAsync(CoreCognitiveCycleEvent evt, CancellationToken ct = default) => Task.CompletedTask;
        public Task EmitBatchAsync(IReadOnlyList<CoreCognitiveCycleEvent> events, CancellationToken ct = default) => Task.CompletedTask;
        public IDisposable Subscribe(ICognitiveStreamSink sink) { _sinks.Add(sink); return new Subscription(() => _sinks.Remove(sink)); }
        public void EmitTestEvent(CoreCognitiveCycleEvent evt) { foreach (var s in _sinks) s.OnEventAsync(evt, CancellationToken.None); }
        private sealed class Subscription(Action onDispose) : IDisposable { public void Dispose() => onDispose(); }
    }
}
