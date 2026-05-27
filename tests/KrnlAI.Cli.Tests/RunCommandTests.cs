using System.CommandLine;
using KrnlAI.Cli.Commands;

namespace KrnlAI.Cli.Tests;

public sealed class RunCommandTests
{
    [Fact]
    public void RunCommand_Build_ShouldCreateCommand()
    {
        var cmd = new RunCommand().Build();

        cmd.Name.Should().Be("run");
        cmd.Description.Should().Contain("pipe");
    }

    [Fact]
    public void RunCommand_ShouldHavePipeOption()
    {
        var cmd = new RunCommand().Build();
        var root = new RootCommand { cmd };
        var result = root.Parse("run \"test\" --pipe");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void RunCommand_ShouldHaveJsonOption()
    {
        var cmd = new RunCommand().Build();
        var root = new RootCommand { cmd };
        var result = root.Parse("run \"test\" --json");
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("embedded")]
    [InlineData("local-api")]
    [InlineData("remote-api")]
    public void RunCommand_ShouldHaveModeOption(string mode)
    {
        var cmd = new RunCommand().Build();
        var root = new RootCommand { cmd };

        var result = root.Parse($"run \"test\" --mode {mode}");

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void RunCommand_WithNoInput_ShouldBeParseable()
    {
        var cmd = new RunCommand().Build();
        var root = new RootCommand { cmd };

        var parseResult = root.Parse("run");

        parseResult.Errors.Should().BeEmpty();
        parseResult.CommandResult.Command.Name.Should().Be("run");
    }

    [Fact]
    public void RunCommand_ShouldHaveOutputOption()
    {
        var cmd = new RunCommand().Build();
        var root = new RootCommand { cmd };
        var result = root.Parse("run \"test\" --output out.txt");
        Assert.Empty(result.Errors);
    }
}
