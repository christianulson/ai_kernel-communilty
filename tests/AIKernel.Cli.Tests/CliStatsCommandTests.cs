using AIKernel.Cli.Commands;

namespace AIKernel.Cli.Tests;

public sealed class CliStatsCommandTests
{
    [Fact]
    public void CliStatsCommand_Build_ShouldReturnCommand()
    {
        var cmd = new CliStatsCommand().Build();
        Assert.NotNull(cmd);
        Assert.Equal("stats", cmd.Name);
        Assert.Contains("--verbose", cmd.Options.Select(o => o.Name));
    }
}
