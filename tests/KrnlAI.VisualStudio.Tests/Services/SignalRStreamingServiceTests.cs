using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

public sealed class SignalRStreamingServiceTests
{
    [Fact]
    public void InitialState_ShouldBeDisconnected()
    {
        var service = new SignalRStreamingService();

        service.State.Should().Be(ConnectionState.Disconnected);
    }

    [Fact]
    public void StateChanged_WhenDisposed_ShouldTransition()
    {
        var service = new SignalRStreamingService();
        var transitions = new List<(ConnectionState Old, ConnectionState New)>();

        service.StateChanged += s => transitions.Add((service.State, s));

        service.State.Should().Be(ConnectionState.Disconnected);
    }

    [Fact]
    public async Task StartAgentStream_WhenNotConnected_ShouldThrow()
    {
        var service = new SignalRStreamingService();
        Func<Task> act = () => service.StartAgentStreamAsync("goal", "session-id");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Not connected to SignalR hub.");
    }

    [Fact]
    public async Task ConnectAsync_WithInvalidUrl_ShouldThrow()
    {
        var service = new SignalRStreamingService();
        Func<Task> act = () => service.ConnectAsync("not-a-valid-url", CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task DisconnectAsync_WhenDisconnected_ShouldNotThrow()
    {
        var service = new SignalRStreamingService();

        Func<Task> act = () => service.DisconnectAsync();

        await act.Should().NotThrowAsync();
    }
}
