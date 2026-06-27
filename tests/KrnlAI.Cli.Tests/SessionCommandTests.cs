using KrnlAI.Contracts;
using AutoFixture;
using System.CommandLine;
using KrnlAI.Cli.Services;
using KrnlAI.Cognition.Contracts;
using KrnlAI.Cognition.Infrastructure;
using Spectre.Console.Testing;
using TestHelpers;
using KrnlAI.Cli.Commands;

namespace KrnlAI.Cli.Tests;

public sealed class SessionCommandTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();

    private static CognitiveSession MakeSession(
        string sessionId, string? goalId, string? userId,
        string[] cycleIds, DateTimeOffset startedAt, DateTimeOffset? completedAt,
        CognitiveSessionStatus status, string summary) =>
        Fixture.Build<CognitiveSession>()
            .With(x => x.SessionId, sessionId)
            .With(x => x.GoalId, goalId)
            .With(x => x.UserId, userId)
            .With(x => x.CycleIds, (IReadOnlyList<string>)cycleIds)
            .With(x => x.StartedAt, startedAt)
            .With(x => x.CompletedAt, completedAt)
            .With(x => x.Status, status)
            .With(x => x.Summary, summary)
            .Create();
    [Fact]
    public async Task SessionCommand_Fork_ShouldCallStore()
    {
        var console = new TestConsole();
        var sessionStore = new InMemorySessionStore();
        var cognitiveStore = new InMemoryCognitiveSessionStore();

        var session = MakeSession("session-fork-1", null, null, [], DateTimeOffset.UtcNow, null, CognitiveSessionStatus.Active, "fork test");
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

        var session = MakeSession("session-show-1", "goal-1", "user-1", ["cycle-1"],
            new DateTimeOffset(2026, 6, 12, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 12, 10, 5, 0, TimeSpan.Zero),
            CognitiveSessionStatus.Completed, "show test session");
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
