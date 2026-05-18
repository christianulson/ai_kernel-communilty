using KrnlAI.Cli.Commands;

namespace KrnlAI.Cli.Tests;

public sealed class ReviewCommandTests
{
    [Fact]
    public void ReviewCommand_Build_ShouldReturnCommand()
    {
        var cmd = new ReviewCommand().Build();
        Assert.NotNull(cmd);
        Assert.Equal("review", cmd.Name);
    }

    [Fact]
    public void ReviewCommand_ShouldHaveEndpointOption()
    {
        var cmd = new ReviewCommand().Build();
        Assert.Contains("--endpoint", cmd.Options.Select(o => o.Name));
    }

    [Fact]
    public void ReviewCommand_ShouldHaveOutputOption()
    {
        var cmd = new ReviewCommand().Build();
        Assert.Contains("--output", cmd.Options.Select(o => o.Name));
    }

    [Fact]
    public void ReviewCommand_ShouldHavePathOption()
    {
        var cmd = new ReviewCommand().Build();
        Assert.Contains("--path", cmd.Options.Select(o => o.Name));
    }
}
