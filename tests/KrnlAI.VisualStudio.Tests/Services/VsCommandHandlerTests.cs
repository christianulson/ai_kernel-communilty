using KrnlAI.VisualStudio.Commands.ChatCommands;
using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Tests.Services;

[Trait("Category", "Unit")]
public sealed class VsCommandHandlerTests
{
    [Fact]
    public void SlashCommandRouter_IsSlashCommand_WithSlash_ShouldReturnTrue()
    {
        var router = CreateRouter();
        Assert.True(router.IsSlashCommand("/explain"));
    }

    [Fact]
    public void SlashCommandRouter_IsSlashCommand_WithoutSlash_ShouldReturnFalse()
    {
        var router = CreateRouter();
        Assert.False(router.IsSlashCommand("explain"));
    }

    [Fact]
    public void SlashCommandRouter_IsSlashCommand_EmptyString_ShouldReturnFalse()
    {
        var router = CreateRouter();
        Assert.False(router.IsSlashCommand(""));
        Assert.False(router.IsSlashCommand(null));
    }

    [Fact]
    public void SlashCommandRouter_Resolve_KnownCommand_ShouldReturnCommand()
    {
        var router = CreateRouter();
        var cmd = router.Resolve("/help");
        Assert.NotNull(cmd);
        Assert.Equal("help", cmd.Name);
    }

    [Fact]
    public void SlashCommandRouter_Resolve_UnknownCommand_ShouldReturnNull()
    {
        var router = CreateRouter();
        var cmd = router.Resolve("/nonexistent");
        Assert.Null(cmd);
    }

    [Fact]
    public void SlashCommandRouter_Resolve_CaseInsensitive_ShouldMatch()
    {
        var router = CreateRouter();
        var cmd = router.Resolve("/HELP");
        Assert.NotNull(cmd);
        Assert.Equal("help", cmd.Name);
    }

    [Fact]
    public void SlashCommandRouter_ExecuteAsync_KnownCommand_ShouldReturnResult()
    {
        var router = CreateRouter();
        var result = router.ExecuteAsync("/help", CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public void SlashCommandRouter_ExecuteAsync_UnknownCommand_ShouldReturnError()
    {
        var router = CreateRouter();
        var result = router.ExecuteAsync("/unknown", CancellationToken.None);

        Assert.Equal("Unknown command: /unknown. Type /help for available commands.", result.Result);
    }

    [Fact]
    public void SlashCommandRouter_Commands_DefaultRegistration_ShouldContainCoreCommands()
    {
        var router = CreateRouter();

        Assert.Contains("explain", router.Commands.Keys);
        Assert.Contains("fix", router.Commands.Keys);
        Assert.Contains("test", router.Commands.Keys);
        Assert.Contains("refactor", router.Commands.Keys);
        Assert.Contains("help", router.Commands.Keys);
    }

    [Fact]
    public void SlashCommandRouter_Commands_ShouldContainGitCommands()
    {
        var router = CreateRouter();

        Assert.Contains("status", router.Commands.Keys);
        Assert.Contains("diff", router.Commands.Keys);
        Assert.Contains("commit", router.Commands.Keys);
    }

    [Fact]
    public void SlashCommandRouter_Command_Help_HasDescription()
    {
        var router = CreateRouter();
        var cmd = router.Resolve("/help");

        Assert.NotNull(cmd);
        Assert.False(string.IsNullOrEmpty(cmd.Description));
    }

    [Fact]
    public void SlashCommand_Constructor_ShouldSetProperties()
    {
        var cmd = new SlashCommand(
            "testcmd",
            "A test command",
            (args, ct) => Task.FromResult("done")
        );

        Assert.Equal("testcmd", cmd.Name);
        Assert.Equal("A test command", cmd.Description);
        Assert.NotNull(cmd.Handler);
        Assert.Null(cmd.IsVisible);
    }

    [Fact]
    public void SlashCommand_WithVisibility_ShouldRespectCondition()
    {
        var visible = false;
        var cmd = new SlashCommand(
            "hidden",
            "Hidden command",
            (args, ct) => Task.FromResult("done"),
            () => visible
        );

        Assert.NotNull(cmd.IsVisible);
        Assert.False(cmd.IsVisible());

        visible = true;
        Assert.True(cmd.IsVisible());
    }

    private static SlashCommandRouter CreateRouter()
    {
        var client = new FakeKernelClient();
        var context = new FakeSolutionContext();
        var applyEdit = new FakeApplyEdit();
        var agenticLoop = new FakeAgenticLoop();
        return new SlashCommandRouter(client, context, applyEdit, agenticLoop);
    }
}

public sealed class FakeKernelClient : IKernelClientService
{
    public ConnectionState State => ConnectionState.Connected;
    public string? BaseUrl => "http://localhost:5235";
    public event Action<ConnectionState>? StateChanged;

    public Task<bool> ConnectAsync(string endpoint, CancellationToken ct = default)
        => Task.FromResult(true);

    public Task DisconnectAsync()
        => Task.CompletedTask;

    public Task<AgentRunResponse> RunAgentAsync(string goal, AgentRunRequest? request = null, CancellationToken ct = default)
        => Task.FromResult(new AgentRunResponse(
            Goal: goal,
            Status: "completed",
            Summary: "Agent completed successfully",
            Steps: Array.Empty<PlanStepResult>(),
            EpisodeId: "ep-1",
            Provider: "test",
            Model: "test-model",
            GoalId: null
        ));

    public Task<MemorySearchResponse> SearchMemoryAsync(string query, int topK = 10, CancellationToken ct = default)
        => Task.FromResult(new MemorySearchResponse(
            Ok: true,
            Hits: Array.Empty<MemorySearchHit>()
        ));

    public Task<HealthStatus> CheckHealthAsync(CancellationToken ct = default)
        => Task.FromResult(new HealthStatus(Ok: true, Ts: DateTime.UtcNow.ToString("O")));

    public Task<string?> GetEmotionalMoodAsync(CancellationToken ct = default)
        => Task.FromResult<string?>("Neutral");
}

public sealed class FakeSolutionContext : ISolutionContextService
{
    public CodeSelection? GetActiveSelection()
        => new CodeSelection("test.cs", ".cs", "C#", "public class Foo {}", "public class Foo {}", 1, 1);

    public string? GetActiveFilePath()
        => "test.cs";

    public string? GetSolutionDirectory()
        => @"C:\test\solution";
}

public sealed class FakeApplyEdit : IApplyEditService
{
    public Task<bool> PreviewAndApplyAsync(string diff, CancellationToken ct)
        => Task.FromResult(true);

    public Task<ApplyEditResult> PreviewDiffAsync(string diff, CancellationToken ct)
        => Task.FromResult(new ApplyEditResult(true, diff, null));

    public Task<bool> ApplyAsync(string diff, CancellationToken ct)
        => Task.FromResult(true);

    public Task UndoAsync()
        => Task.CompletedTask;
}

public sealed class FakeAgenticLoop : IAgenticLoopService
{
    public Task<AgenticLoopResult> ExecuteAsync(string goal, CancellationToken ct)
        => Task.FromResult(new AgenticLoopResult("completed", "Done", null, null));

    public Task CancelAsync()
        => Task.CompletedTask;
}
