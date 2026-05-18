using KrnlAI.Cli.Commands;

namespace KrnlAI.Cli.Tests;

public sealed class EvalCommandTests
{
    [Fact]
    public void EvalCommand_Build_ShouldReturnCommand()
    {
        var cmd = new EvalCommand().Build();
        Assert.NotNull(cmd);
        Assert.Equal("eval", cmd.Name);
    }

    [Fact]
    public void EvalCommand_ShouldHaveFileArgument()
    {
        var cmd = new EvalCommand().Build();
        Assert.Contains("file", cmd.Arguments.Select(a => a.Name));
    }

    [Fact]
    public void EvalCommand_ShouldHaveEndpointOption()
    {
        var cmd = new EvalCommand().Build();
        Assert.Contains("--endpoint", cmd.Options.Select(o => o.Name));
    }

    [Fact]
    public void EvalCommand_ShouldHaveTimeoutOption()
    {
        var cmd = new EvalCommand().Build();
        Assert.Contains("--timeout", cmd.Options.Select(o => o.Name));
    }

    [Fact]
    public void EvalCommand_ShouldHaveOutputOption()
    {
        var cmd = new EvalCommand().Build();
        Assert.Contains("--output", cmd.Options.Select(o => o.Name));
    }
}
