using System.CommandLine;
using KrnlAI.Cli.Commands;
using KrnlAI.Cli.Services;
using KrnlAI.Cognition.Contracts;
using KrnlAI.Cognition.Infrastructure;
using Spectre.Console.Testing;

namespace KrnlAI.Cli.Tests;

public sealed class SessionCommandTests
{
    [Fact]
    public async Task SessionCommand_Fork_ShouldCallStore()
    {
        var console = new TestConsole();
        var sessionStore = new InMemorySessionStore();
        var cognitiveStore = new InMemoryCognitiveSessionStore();

        var session = new CognitiveSession(
            SessionId: "session-fork-1",
            GoalId: null,
            UserId: null,
            CycleIds: [],
            StartedAt: DateTimeOffset.UtcNow,
            CompletedAt: null,
            Status: CognitiveSessionStatus.Active,
            Summary: "fork test");
        await cognitiveStore.SaveAsync(session, TestContext.Current.CancellationToken);

        var cmd = new SessionCommand(console, sessionStore, cognitiveStore).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("session fork session-fork-1").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("Session forked");
        console.Output.Should().Contain("session-fork-1");
    }

    [Fact]
    public async Task SessionCommand_List_ShouldReturnSessions()
    {
        var console = new TestConsole();
        var sessionStore = new InMemorySessionStore();
        var cognitiveStore = new InMemoryCognitiveSessionStore();

        sessionStore.Create("test-session-1");
        sessionStore.Create("test-session-2");

        var cmd = new SessionCommand(console, sessionStore, cognitiveStore).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("session list").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("test-session-1");
        console.Output.Should().Contain("test-session-2");
    }

    [Fact]
    public async Task SessionCommand_Show_ShouldDisplayDetails()
    {
        var console = new TestConsole();
        var sessionStore = new InMemorySessionStore();
        var cognitiveStore = new InMemoryCognitiveSessionStore();

        var session = new CognitiveSession(
            SessionId: "session-show-1",
            GoalId: "goal-1",
            UserId: "user-1",
            CycleIds: ["cycle-1"],
            StartedAt: new DateTimeOffset(2026, 6, 12, 10, 0, 0, TimeSpan.Zero),
            CompletedAt: new DateTimeOffset(2026, 6, 12, 10, 5, 0, TimeSpan.Zero),
            Status: CognitiveSessionStatus.Completed,
            Summary: "show test session");
        await cognitiveStore.SaveAsync(session, TestContext.Current.CancellationToken);

        var cmd = new SessionCommand(console, sessionStore, cognitiveStore).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("session show session-show-1").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("session-show-1");
        console.Output.Should().Contain("Completed");
        console.Output.Should().Contain("show test session");
    }
}
