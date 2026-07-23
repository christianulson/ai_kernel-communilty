using KrnlAI.Desktop.App.Services;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class HttpCognitiveStreamProviderTests
{
    private static HttpCognitiveStreamProvider CreateProvider() =>
        new(new HttpClient { BaseAddress = new Uri("http://localhost") });

    [Fact]
    public async Task ConnectAsync_ShouldInvokeOnStateChanged()
    {
        var stateChanges = new List<CognitiveStreamState>();
        var provider = CreateProvider();
        provider.OnStateChanged += s => stateChanges.Add(s);

        await provider.ConnectAsync("cycle-1", CancellationToken.None);

        Assert.Contains(CognitiveStreamState.Connecting, stateChanges);
        Assert.Contains(CognitiveStreamState.Connected, stateChanges);
    }

    [Fact]
    public async Task ConnectAsync_WithoutCycleId_ShouldStillConnect()
    {
        var stateChanges = new List<CognitiveStreamState>();
        var provider = CreateProvider();
        provider.OnStateChanged += s => stateChanges.Add(s);

        await provider.ConnectAsync(ct: CancellationToken.None);

        Assert.Contains(CognitiveStreamState.Connected, stateChanges);
    }

    [Fact]
    public async Task ConnectAsync_EventsProperty_ShouldStartEmpty()
    {
        var provider = CreateProvider();
        Assert.Empty(provider.Events);

        await provider.ConnectAsync("cycle-1", CancellationToken.None);

        Assert.Empty(provider.Events); // No events polled yet
    }

    [Fact]
    public void Disconnect_ShouldSetDisconnectedState()
    {
        var stateChanges = new List<CognitiveStreamState>();
        var provider = CreateProvider();
        provider.OnStateChanged += s => stateChanges.Add(s);

        provider.Disconnect();

        Assert.Contains(CognitiveStreamState.Disconnected, stateChanges);
    }

    [Fact]
    public void Disconnect_WhenNotConnected_ShouldNotThrow()
    {
        var provider = CreateProvider();
        provider.Disconnect();
    }

    [Fact]
    public void Disconnect_MultipleCalls_ShouldNotThrow()
    {
        var provider = CreateProvider();
        provider.Disconnect();
        provider.Disconnect();
    }

    [Fact]
    public async Task ConnectAsync_ThenDisconnect_ShouldTransitionThroughAllStates()
    {
        var stateChanges = new List<CognitiveStreamState>();
        var provider = CreateProvider();
        provider.OnStateChanged += s => stateChanges.Add(s);

        await provider.ConnectAsync("cycle-1", CancellationToken.None);
        provider.Disconnect();

        Assert.Equal(CognitiveStreamState.Connecting, stateChanges[0]);
        Assert.Equal(CognitiveStreamState.Connected, stateChanges[1]);
        Assert.Equal(CognitiveStreamState.Disconnected, stateChanges[2]);
    }

    [Fact]
    public async Task ConnectAsync_Disconnect_ShouldClearEvents()
    {
        var provider = CreateProvider();
        await provider.ConnectAsync("cycle-1", CancellationToken.None);
        provider.Disconnect();

        Assert.Equal(CognitiveStreamState.Disconnected, provider.State);
    }

    [Fact]
    public async Task ConnectAsync_AfterDisconnect_ShouldReconnect()
    {
        var provider = CreateProvider();
        await provider.ConnectAsync("cycle-1", CancellationToken.None);
        provider.Disconnect();
        await provider.ConnectAsync("cycle-2", CancellationToken.None);

        Assert.Equal(CognitiveStreamState.Connected, provider.State);
    }

    [Fact]
    public void State_Initially_ShouldBeDisconnected()
    {
        var provider = CreateProvider();
        Assert.Equal(CognitiveStreamState.Disconnected, provider.State);
    }
}
