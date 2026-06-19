using AutoFixture;
using AutoFixture.AutoMoq;
using KrnlAI.VisualStudio.Commands.ChatCommands;
using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Commands;

public sealed class SlashCommandRouterTests
{
    private static readonly IFixture Fixture = new Fixture().Customize(new AutoMoqCustomization());
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
        var context = Mock.Of<ISolutionContextService>();
        var applyEdit = Mock.Of<IApplyEditService>();
        var agenticLoop = Mock.Of<IAgenticLoopService>();
        return new SlashCommandRouter(client, context, applyEdit, agenticLoop);
    }
}
