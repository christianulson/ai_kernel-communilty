using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

public sealed class SessionTeleportServiceTests
{
    private static SessionTeleportService CreateService() =>
        new(new InMemorySessionStorage());

    [Fact]
    public async Task SaveAndRestore_ShouldRoundTrip()
    {
        var service = CreateService();
        var state = new SessionState("s1", "goal1", ["file1.cs"], 5, DateTime.UtcNow);

        await service.SaveSessionAsync(state);
        var restored = await service.RestoreSessionAsync();

        restored.Should().NotBeNull();
        restored!.SessionId.Should().Be("s1");
        restored.LastGoal.Should().Be("goal1");
        restored.ContextFiles.Should().Contain("file1.cs");
        restored.MessageCount.Should().Be(5);
    }

    [Fact]
    public async Task ClearSession_ShouldRemoveState()
    {
        var service = CreateService();
        await service.SaveSessionAsync(new SessionState("s1", null, [], 0, DateTime.UtcNow));

        await service.ClearSessionAsync();
        var restored = await service.RestoreSessionAsync();

        restored.Should().BeNull();
        service.CurrentSession.Should().BeNull();
    }

    [Fact]
    public void CurrentSession_ShouldBeNullInitially()
    {
        var service = CreateService();
        service.CurrentSession.Should().BeNull();
    }

    [Fact]
    public async Task SaveSession_ShouldUpdateCurrentSession()
    {
        var service = CreateService();
        var state = new SessionState("s2", "goal2", [], 3, DateTime.UtcNow);

        await service.SaveSessionAsync(state);

        service.CurrentSession.Should().NotBeNull();
        service.CurrentSession!.SessionId.Should().Be("s2");
    }

    [Fact]
    public async Task MultipleSaves_ShouldOverwrite()
    {
        var service = CreateService();
        await service.SaveSessionAsync(new SessionState("s1", "g1", [], 0, DateTime.UtcNow));
        await service.SaveSessionAsync(new SessionState("s2", "g2", [], 0, DateTime.UtcNow));

        var restored = await service.RestoreSessionAsync();

        restored!.SessionId.Should().Be("s2");
        restored.LastGoal.Should().Be("g2");
    }

    [Fact]
    public async Task Restore_WhenEmpty_ShouldReturnNull()
    {
        var service = CreateService();
        var restored = await service.RestoreSessionAsync();

        restored.Should().BeNull();
    }
}
