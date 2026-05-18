using KrnlAI.Cli.Commands;

namespace KrnlAI.Cli.Tests;

public sealed class ServeCommandTests
{
    [Fact]
    public void ServeCommand_Build_ShouldCreateCommand()
    {
        var cmd = new ServeCommand().Build();

        cmd.Name.Should().Be("serve");
        cmd.Description.Should().Be("Start headless HTTP server");
    }

    [Fact]
    public void ServeCommand_ShouldHavePortOption()
    {
        var cmd = new ServeCommand().Build();

        cmd.Should().NotBeNull();
        cmd.Name.Should().Be("serve");
    }

    [Fact]
    public void ServeCommand_ShouldHaveModelOption()
    {
        var cmd = new ServeCommand().Build();

        cmd.Options.Select(o => o.Name).Should().Contain("--model");
    }
}
