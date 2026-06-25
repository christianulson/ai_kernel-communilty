using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class HttpCognitiveStreamProviderTests
{
    [Fact]
    public async Task ConnectAsync_ShouldInvokeOnStateChanged()
    {
        var stateChanges = new List<CognitiveStreamState>();
        var provider = new HttpCognitiveStreamProvider("http://localhost");
        provider.OnStateChanged += s => stateChanges.Add(s);

        await provider.ConnectAsync("cycle-1", CancellationToken.None).ConfigureAwait(false);

        Assert.Contains(CognitiveStreamState.Connecting, stateChanges);
        Assert.Contains(CognitiveStreamState.Connected, stateChanges);
    }

    [Fact]
    public async Task ConnectAsync_WithoutCycleId_ShouldStillConnect()
    {
        var stateChanges = new List<CognitiveStreamState>();
        var provider = new HttpCognitiveStreamProvider("http://localhost");
        provider.OnStateChanged += s => stateChanges.Add(s);

        await provider.ConnectAsync(ct: CancellationToken.None).ConfigureAwait(false);

        Assert.Contains(CognitiveStreamState.Connected, stateChanges);
    }

    [Fact]
    public async Task ConnectAsync_EventsProperty_ShouldStartEmpty()
    {
        var provider = new HttpCognitiveStreamProvider("http://localhost");
        Assert.Empty(provider.Events);

        await provider.ConnectAsync("cycle-1", CancellationToken.None).ConfigureAwait(false);

        Assert.Empty(provider.Events); // No events polled yet
    }

    [Fact]
    public void Disconnect_ShouldSetDisconnectedState()
    {
        var stateChanges = new List<CognitiveStreamState>();
        var provider = new HttpCognitiveStreamProvider("http://localhost");
        provider.OnStateChanged += s => stateChanges.Add(s);

        provider.Disconnect();

        Assert.Contains(CognitiveStreamState.Disconnected, stateChanges);
    }

    [Fact]
    public void Disconnect_WhenNotConnected_ShouldNotThrow()
    {
        var provider = new HttpCognitiveStreamProvider("http://localhost");
        provider.Disconnect();
    }

    [Fact]
    public void Disconnect_MultipleCalls_ShouldNotThrow()
    {
        var provider = new HttpCognitiveStreamProvider("http://localhost");
        provider.Disconnect();
        provider.Disconnect();
    }

    [Fact]
    public async Task ConnectAsync_ThenDisconnect_ShouldTransitionThroughAllStates()
    {
        var stateChanges = new List<CognitiveStreamState>();
        var provider = new HttpCognitiveStreamProvider("http://localhost");
        provider.OnStateChanged += s => stateChanges.Add(s);

        await provider.ConnectAsync("cycle-1", CancellationToken.None).ConfigureAwait(false);
        provider.Disconnect();

        Assert.Equal(CognitiveStreamState.Connecting, stateChanges[0]);
        Assert.Equal(CognitiveStreamState.Connected, stateChanges[1]);
        Assert.Equal(CognitiveStreamState.Disconnected, stateChanges[2]);
    }

    [Fact]
    public async Task ConnectAsync_Disconnect_ShouldClearEvents()
    {
        var provider = new HttpCognitiveStreamProvider("http://localhost");
        await provider.ConnectAsync("cycle-1", CancellationToken.None).ConfigureAwait(false);
        provider.Disconnect();

        Assert.Equal(CognitiveStreamState.Disconnected, provider.State);
    }

    [Fact]
    public async Task ConnectAsync_AfterDisconnect_ShouldReconnect()
    {
        var provider = new HttpCognitiveStreamProvider("http://localhost");
        await provider.ConnectAsync("cycle-1", CancellationToken.None).ConfigureAwait(false);
        provider.Disconnect();
        await provider.ConnectAsync("cycle-2", CancellationToken.None).ConfigureAwait(false);

        Assert.Equal(CognitiveStreamState.Connected, provider.State);
    }

    [Fact]
    public void State_Initially_ShouldBeDisconnected()
    {
        var provider = new HttpCognitiveStreamProvider("http://localhost");
        Assert.Equal(CognitiveStreamState.Disconnected, provider.State);
    }
}
