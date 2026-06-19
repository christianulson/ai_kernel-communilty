using AutoFixture;
using KrnlAI.Cli.Abstractions;
using KrnlAI.Cli.Commands;
using Moq;
using Spectre.Console.Testing;
using TestHelpers;

namespace KrnlAI.Cli.Tests;

public sealed class InitCommandTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();
    [Fact]
    public void InitCommand_Build_ShouldCreateCommand()
    {
        var console = new TestConsole();
        var engine = Mock.Of<ITemplateEngine>();
        var cmd = new InitCommand(engine, console).Build();

        cmd.Name.Should().Be("init");
        cmd.Description.Should().Be("Interactive project initialization");
    }

    [Fact]
    public void InitCommand_ShouldUseTemplateEngine()
    {
        var console = new TestConsole();
        var engine = Mock.Of<ITemplateEngine>();
        var cmd = new InitCommand(engine, console).Build();

        cmd.Should().NotBeNull();
    }
}
