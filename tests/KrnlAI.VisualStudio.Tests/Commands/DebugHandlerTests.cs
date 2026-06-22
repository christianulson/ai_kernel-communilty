using KrnlAI.VisualStudio.Commands.ChatCommands;
using KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;
using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Commands;

[Trait("Category", "Unit")]
public sealed class DebugHandlerTests
{
    [Fact]
    public void Create_ShouldReturnSlashCommand()
    {
        var tracker = new VsOperationTracker();
        var cmd = DebugHandler.Create(tracker);

        cmd.Name.Should().Be("debug");
        cmd.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_EmptyHistory_ShouldReturnMessage()
    {
        var tracker = new VsOperationTracker();
        var cmd = DebugHandler.Create(tracker);

        var result = await cmd.Handler("", CancellationToken.None);

        result.Should().Contain("No operations");
    }

    [Fact]
    public async Task Handle_WithOperations_ShouldShowThem()
    {
        var tracker = new VsOperationTracker();
        using (var op = tracker.Start("test.op", "arg1"))
        {
            op.SetResult("done");
        }

        var cmd = DebugHandler.Create(tracker);
        var result = await cmd.Handler("", CancellationToken.None);

        result.Should().Contain("test.op");
        result.Should().Contain("arg1");
        result.Should().Contain("Completed");
    }

    [Fact]
    public async Task Handle_Clear_ShouldClearHistory()
    {
        var tracker = new VsOperationTracker();
        using (var op = tracker.Start("test")) { op.SetResult("ok"); }
        tracker.History.Should().HaveCount(1);

        var cmd = DebugHandler.Create(tracker);
        var result = await cmd.Handler("clear", CancellationToken.None);

        result.Should().Contain("cleared");
        tracker.History.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithLimit_ShouldShowLastN()
    {
        var tracker = new VsOperationTracker();
        for (var i = 0; i < 5; i++)
        {
            using var op = tracker.Start($"op-{i}");
            op.SetResult("ok");
        }

        var cmd = DebugHandler.Create(tracker);
        var result = await cmd.Handler("3", CancellationToken.None);

        result.Should().Contain("op-2");
        result.Should().Contain("op-3");
        result.Should().Contain("op-4");
        result.Should().NotContain("op-0");
        result.Should().NotContain("op-1");
    }

    [Fact]
    public async Task Handle_WithError_ShouldShowError()
    {
        var tracker = new VsOperationTracker();
        using (var op = tracker.Start("failing"))
        {
            op.SetError("timeout");
        }

        var cmd = DebugHandler.Create(tracker);
        var result = await cmd.Handler("", CancellationToken.None);

        result.Should().Contain("failing");
        result.Should().Contain("Failed");
        result.Should().Contain("timeout");
    }

    [Fact]
    public async Task Handle_DebugHelp_ShouldShowUsage()
    {
        var tracker = new VsOperationTracker();
        var cmd = DebugHandler.Create(tracker);

        var result = await cmd.Handler("help", CancellationToken.None);

        result.Should().Contain("Usage");
        result.Should().Contain("/debug");
        result.Should().Contain("clear");
    }
}
