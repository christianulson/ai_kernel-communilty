using KrnlAI.Desktop.Core.Models;
using Cts = KrnlAI.Contracts.Contracts;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class OfflineServiceTests
{
    [Fact]
    public void IsOffline_Default_ShouldBeFalse()
    {
        var sut = new OfflineService();

        Assert.False(sut.IsOffline);
    }

    [Fact]
    public void SetOfflineStatus_ToTrue_ShouldRaiseConnectivityChanged()
    {
        var sut = new OfflineService();
        var raised = false;

        sut.ConnectivityChanged += (_, _) => raised = true;
        sut.SetOfflineStatus(true);

        Assert.True(sut.IsOffline);
        Assert.True(raised);
    }

    [Fact]
    public void SetOfflineStatus_SameValue_ShouldNotRaiseEvent()
    {
        var sut = new OfflineService();
        var count = 0;

        sut.ConnectivityChanged += (_, _) => count++;
        sut.SetOfflineStatus(false);

        Assert.Equal(0, count);
    }

    [Fact]
    public void SetOfflineStatus_Toggle_ShouldRaiseTwice()
    {
        var sut = new OfflineService();
        var count = 0;

        sut.ConnectivityChanged += (_, _) => count++;
        sut.SetOfflineStatus(true);
        sut.SetOfflineStatus(false);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CacheCommandAsync_ShouldStoreCommand()
    {
        var sut = new OfflineService();
        var request = new Cts.AgentRunTransportRequest("test action");

        var result = await sut.CacheCommandAsync(request).ConfigureAwait(false);

        Assert.True(result);
    }

    [Fact]
    public async Task GetCachedCommandsAsync_AfterCache_ShouldReturnCached()
    {
        var sut = new OfflineService();
        var request = new Cts.AgentRunTransportRequest("test action");
        await sut.CacheCommandAsync(request).ConfigureAwait(false);

        var cached = await sut.GetCachedCommandsAsync().ConfigureAwait(false);

        Assert.Single(cached);
        Assert.Equal("test action", cached[0].Prompt);
    }

    [Fact]
    public async Task GetCachedCommandsAsync_Empty_ShouldReturnEmpty()
    {
        var sut = new OfflineService();

        var cached = await sut.GetCachedCommandsAsync().ConfigureAwait(false);

        Assert.Empty(cached);
    }

    [Fact]
    public async Task ClearCacheAsync_ShouldRemoveAllCommands()
    {
        var sut = new OfflineService();
        await sut.CacheCommandAsync(new Cts.AgentRunTransportRequest("first cmd")).ConfigureAwait(false);
        await sut.CacheCommandAsync(new Cts.AgentRunTransportRequest("second cmd")).ConfigureAwait(false);

        await sut.ClearCacheAsync().ConfigureAwait(false);

        var cached = await sut.GetCachedCommandsAsync().ConfigureAwait(false);
        Assert.Empty(cached);
    }

    [Fact]
    public async Task CacheCommandAsync_Multiple_ShouldPreserveOrder()
    {
        var sut = new OfflineService();

        await sut.CacheCommandAsync(new Cts.AgentRunTransportRequest("first cmd")).ConfigureAwait(false);
        await sut.CacheCommandAsync(new Cts.AgentRunTransportRequest("second cmd")).ConfigureAwait(false);

        var cached = await sut.GetCachedCommandsAsync().ConfigureAwait(false);

        Assert.Equal(2, cached.Count);
        Assert.Equal("first cmd", cached[0].Prompt);
        Assert.Equal("second cmd", cached[1].Prompt);
    }
}
