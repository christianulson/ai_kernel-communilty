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

        await provider.ConnectAsync("cycle-1", CancellationToken.None);

        Assert.Contains(CognitiveStreamState.Connecting, stateChanges);
        Assert.Contains(CognitiveStreamState.Connected, stateChanges);
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
    public async Task ConnectAsync_ThenDisconnect_ShouldTransitionThroughAllStates()
    {
        var stateChanges = new List<CognitiveStreamState>();
        var provider = new HttpCognitiveStreamProvider("http://localhost");
        provider.OnStateChanged += s => stateChanges.Add(s);

        await provider.ConnectAsync("cycle-1", CancellationToken.None);
        provider.Disconnect();

        Assert.Contains(CognitiveStreamState.Connecting, stateChanges);
        Assert.Contains(CognitiveStreamState.Connected, stateChanges);
        Assert.Contains(CognitiveStreamState.Disconnected, stateChanges);
    }
}
