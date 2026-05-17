using AIKernel.Cli.Commands;

namespace AIKernel.Cli.Tests;

public sealed class RunCommandTests
{
    [Fact]
    public void RunCommand_Build_ShouldReturnCommand()
    {
        var cmd = new RunCommand().Build();
        Assert.NotNull(cmd);
        Assert.Equal("run", cmd.Name);
    }

    [Fact]
    public void RunCommand_HasOptions()
    {
        var cmd = new RunCommand().Build();
        Assert.True(cmd.Options.Count > 0, "Should have at least one option");
    }
}
