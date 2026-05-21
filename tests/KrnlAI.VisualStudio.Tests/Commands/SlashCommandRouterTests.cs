using KrnlAI.VisualStudio.Commands.ChatCommands;
using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Commands;

public sealed class SlashCommandRouterTests
{
    [Fact]
    public void IsSlashCommand_WithSlashPrefix_ShouldReturnTrue()
    {
        var router = CreateRouter();
        router.IsSlashCommand("/explain").Should().BeTrue();
        router.IsSlashCommand("/fix").Should().BeTrue();
    }

    [Fact]
    public void IsSlashCommand_WithoutSlashPrefix_ShouldReturnFalse()
    {
        var router = CreateRouter();
        router.IsSlashCommand("hello").Should().BeFalse();
        router.IsSlashCommand(" explain").Should().BeFalse();
        router.IsSlashCommand("").Should().BeFalse();
    }

    [Fact]
    public void Resolve_WithValidCommand_ShouldReturnCommand()
    {
        var router = CreateRouter();
        var cmd = router.Resolve("/explain");
        cmd.Should().NotBeNull();
        cmd!.Name.Should().Be("explain");
    }

    [Fact]
    public void Resolve_WithUnknownCommand_ShouldReturnNull()
    {
        var router = CreateRouter();
        var cmd = router.Resolve("/nonexistent");
        cmd.Should().BeNull();
    }

    [Fact]
    public void Resolve_WithCaseInsensitive_ShouldMatch()
    {
        var router = CreateRouter();
        router.Resolve("/EXPLAIN").Should().NotBeNull();
        router.Resolve("/Explain").Should().NotBeNull();
    }

    [Fact]
    public void GetVisibleCommands_ShouldReturnAll()
    {
        var router = CreateRouter();
        var cmds = router.GetVisibleCommands();
        cmds.Should().Contain(c => c.Name == "explain");
        cmds.Should().Contain(c => c.Name == "fix");
        cmds.Should().Contain(c => c.Name == "test");
        cmds.Should().Contain(c => c.Name == "refactor");
        cmds.Should().Contain(c => c.Name == "review");
        cmds.Should().Contain(c => c.Name == "task");
        cmds.Should().Contain(c => c.Name == "help");
    }

    [Fact]
    public async Task ExecuteAsync_WithExplain_ShouldReturnResult()
    {
        var router = CreateRouter();
        var result = await router.ExecuteAsync("/help", CancellationToken.None);
        result.Should().Contain("/explain");
        result.Should().Contain("/fix");
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownCommand_ShouldReturnError()
    {
        var router = CreateRouter();
        var result = await router.ExecuteAsync("/bogus", CancellationToken.None);
        result.Should().Contain("Unknown command");
    }

    private static SlashCommandRouter CreateRouter()
    {
        var client = new KernelClientService();
        var context = new FakeSolutionContext();
        var applyEdit = new FakeApplyEdit();
        var agenticLoop = new FakeAgenticLoop();
        return new SlashCommandRouter(client, context, applyEdit, agenticLoop);
    }

    private sealed class FakeSolutionContext : ISolutionContextService
    {
        public CodeSelection? GetActiveSelection() =>
            new CodeSelection("test.cs", ".cs", "csharp", "var x = 1;", null, 1, 1);
        public string? GetActiveFilePath() => "test.cs";
        public string? GetSolutionDirectory() => @"C:\Projects\test";
    }

    private sealed class FakeApplyEdit : IApplyEditService
    {
        public Task<bool> PreviewAndApplyAsync(string diff, CancellationToken ct) => Task.FromResult(true);
        public Task<ApplyEditResult> PreviewDiffAsync(string diff, CancellationToken ct)
            => Task.FromResult(new ApplyEditResult(true, diff, null));
        public Task<bool> ApplyAsync(string diff, CancellationToken ct) => Task.FromResult(true);
        public Task UndoAsync() => Task.CompletedTask;
    }

    private sealed class FakeAgenticLoop : IAgenticLoopService
    {
        public Task<AgenticLoopResult> ExecuteAsync(string goal, CancellationToken ct)
            => Task.FromResult(new AgenticLoopResult("Completed", $"Executed: {goal}", null, null));
        public Task CancelAsync() => Task.CompletedTask;
    }
}
