using System.Net;
using KrnlAI.Desktop.App.Services;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class HttpSlashCommandExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_Clear_ShouldReturnConstant()
    {
        var executor = new HttpSlashCommandExecutor("http://localhost");
        Assert.Equal("CLEAR_CONVERSATION", await executor.ExecuteAsync("/clear"));
    }

    [Fact]
    public async Task ExecuteAsync_Help_ShouldReturnHelpText()
    {
        var executor = new HttpSlashCommandExecutor("http://localhost");
        var result = await executor.ExecuteAsync("/help");
        Assert.Contains("/undo", result);
        Assert.Contains("/diff", result);
        Assert.Contains("/run", result);
        Assert.Contains("/test", result);
        Assert.DoesNotContain("Error", result);
    }

    [Fact]
    public async Task ExecuteAsync_Help_WithArgs_ShouldStillShowHelp()
    {
        var executor = new HttpSlashCommandExecutor("http://localhost");
        var result = await executor.ExecuteAsync("/help commands");
        Assert.Contains("/undo", result);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownCommand_ShouldReturnError()
    {
        var executor = new HttpSlashCommandExecutor("http://localhost");
        var result = await executor.ExecuteAsync("/nonexistent123");
        Assert.Contains("Error", result);
    }

    [Fact]
    public async Task ExecuteAsync_EmptySlash_ShouldReturnError()
    {
        var executor = new HttpSlashCommandExecutor("http://localhost");
        var result = await executor.ExecuteAsync("/");
        Assert.Contains("Error", result);
    }

    [Fact]
    public async Task ExecuteAsync_Undo_ApiFailure_ShouldReturnError()
    {
        var executor = new HttpSlashCommandExecutor("http://localhost");
        var result = await executor.ExecuteAsync("/undo");
        Assert.Contains("Error", result);
    }

    [Fact]
    public async Task ExecuteAsync_Run_WithoutArgs_ShouldShowUsage()
    {
        var executor = new HttpSlashCommandExecutor("http://localhost");
        var result = await executor.ExecuteAsync("/run");
        Assert.Contains("Usage", result);
    }

    [Fact]
    public async Task ExecuteAsync_ExplainerCommands_ShouldReturnVsOnlyMessage()
    {
        var executor = new HttpSlashCommandExecutor("http://localhost");
        var result = await executor.ExecuteAsync("/explain");
        Assert.Contains("VS Code", result);
    }

    [Fact]
    public async Task ExecuteAsync_Diff_ApiFailure_ShouldReturnError()
    {
        var executor = new HttpSlashCommandExecutor("http://localhost");
        var result = await executor.ExecuteAsync("/diff");
        Assert.Contains("Error", result);
    }

    [Fact]
    public async Task ExecuteAsync_Commit_WithArgs_ShouldReturnError()
    {
        var executor = new HttpSlashCommandExecutor("http://localhost");
        var result = await executor.ExecuteAsync("/commit fix bug");
        Assert.Contains("Error", result);
    }

    [Fact]
    public async Task IsSlashCommand_SlashPrefix_ShouldReturnTrue()
    {
        var executor = new HttpSlashCommandExecutor("http://localhost");
        Assert.True(executor.IsSlashCommand("/help"));
        Assert.True(executor.IsSlashCommand("/undo something"));
    }

    [Fact]
    public async Task IsSlashCommand_NormalText_ShouldReturnFalse()
    {
        var executor = new HttpSlashCommandExecutor("http://localhost");
        Assert.False(executor.IsSlashCommand("hello"));
        Assert.False(executor.IsSlashCommand(""));
    }
}
