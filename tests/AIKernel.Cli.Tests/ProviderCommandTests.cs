using AIKernel.Cli.Commands;

namespace AIKernel.Cli.Tests;

public sealed class ProviderCommandTests
{
    [Fact]
    public void ProviderCommand_Build_ShouldHaveSubcommands()
    {
        var cmd = new ProviderCommand().Build();
        Assert.NotNull(cmd);
        Assert.Equal("provider", cmd.Name);

        var subCommands = cmd.Subcommands.Select(c => c.Name).ToList();
        Assert.Contains("list", subCommands);
        Assert.Contains("add", subCommands);
        Assert.Contains("remove", subCommands);
    }

    [Fact]
    public void ProviderCommand_AddSubcommand_ShouldHaveOptions()
    {
        var cmd = new ProviderCommand().Build();
        var addCmd = cmd.Subcommands.First(c => c.Name == "add");
        Assert.Contains("name", addCmd.Arguments.Select(a => a.Name));
        Assert.Contains("--api-key", addCmd.Options.Select(o => o.Name));
        Assert.Contains("--model", addCmd.Options.Select(o => o.Name));
        Assert.Contains("--endpoint", addCmd.Options.Select(o => o.Name));
    }
}
