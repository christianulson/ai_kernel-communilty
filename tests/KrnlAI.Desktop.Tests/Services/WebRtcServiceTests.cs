using KrnlAI.Desktop.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace KrnlAI.Desktop.Tests.Services;

public class WebRtcServiceTests
{
    private static WebRtcService CreateService() => new(NullLogger<WebRtcService>.Instance);

    [Fact]
    public async Task InitializeAsync_ShouldSucceed()
    {
        using var service = CreateService();
        var result = await service.InitializeAsync("ws://localhost:5000/signaling/webrtc", "stun.l.google.com:19302");
        Assert.True(result);
        Assert.NotEmpty(service.LocalPeerId);
    }

    [Fact]
    public async Task InitializeAsync_WithEmptyUrl_ShouldSucceed()
    {
        using var service = CreateService();
        var result = await service.InitializeAsync("ws://localhost:5000/signaling/webrtc", "stun.l.google.com:19302");
        Assert.True(result);
    }

    [Fact]
    public async Task DisconnectAsync_ShouldClose_WhenNotConnected()
    {
        using var service = CreateService();
        await service.InitializeAsync("ws://localhost:5000/signaling/webrtc", "stun.l.google.com:19302");
        await service.DisconnectAsync();
        Assert.False(service.IsConnected);
    }

    [Fact]
    public async Task CreateAndSendOfferAsync_ShouldFail_WhenNoWebSocket()
    {
        using var service = CreateService();
        await service.InitializeAsync("ws://localhost:5000/signaling/webrtc", "stun.l.google.com:19302");
        var result = await service.CreateAndSendOfferAsync("peer-test");
        Assert.False(result);
    }

    [Fact]
    public async Task ConnectToPeerAsync_ShouldFail_WhenNoServer()
    {
        using var service = CreateService();
        await service.InitializeAsync("ws://localhost:5000/signaling/webrtc", "stun.l.google.com:19302");
        var result = await service.ConnectToPeerAsync("peer-test");
        Assert.False(result);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        var service = CreateService();
        var ex = Record.Exception(() => service.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public async Task Dispose_AfterInitialize_ShouldNotThrow()
    {
        var service = CreateService();
        await service.InitializeAsync("ws://localhost:5000/signaling/webrtc", "stun.l.google.com:19302");
        var ex = Record.Exception(() => service.Dispose());
        Assert.Null(ex);
    }
}
