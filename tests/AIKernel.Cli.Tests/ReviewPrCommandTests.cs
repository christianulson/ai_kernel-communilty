using AIKernel.Cli.Commands;

namespace AIKernel.Cli.Tests;

public sealed class ReviewPrCommandTests
{
    [Fact]
    public void ReviewPrCommand_Build_ShouldReturnCommand()
    {
        var cmd = new ReviewPrCommand().Build();
        Assert.NotNull(cmd);
        Assert.Equal("review-pr", cmd.Name);
    }

    [Fact]
    public void ReviewPrCommand_ShouldHavePrNumberArgument()
    {
        var cmd = new ReviewPrCommand().Build();
        Assert.Contains("pr-number", cmd.Arguments.Select(a => a.Name));
    }

    [Fact]
    public void ReviewPrCommand_ShouldHaveEndpointOption()
    {
        var cmd = new ReviewPrCommand().Build();
        Assert.Contains("--endpoint", cmd.Options.Select(o => o.Name));
    }

    [Fact]
    public void ReviewPrCommand_ShouldHaveOutputOption()
    {
        var cmd = new ReviewPrCommand().Build();
        Assert.Contains("--output", cmd.Options.Select(o => o.Name));
    }
}
