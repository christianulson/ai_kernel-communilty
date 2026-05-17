using AIKernel.Cli.Commands;

namespace AIKernel.Cli.Tests;

public sealed class ExportCommandTests
{
    [Fact]
    public void ExportCommand_Build_ShouldHaveSubcommands()
    {
        var cmd = new ExportCommand().Build();
        Assert.NotNull(cmd);
        Assert.Equal("export", cmd.Name);

        var subNames = cmd.Subcommands.Select(c => c.Name).ToList();
        Assert.Contains("session", subNames);
        Assert.Contains("config", subNames);
    }

    [Fact]
    public void ExportCommand_SessionSubcommand_ShouldHaveIdArg()
    {
        var cmd = new ExportCommand().Build();
        var sessionCmd = cmd.Subcommands.First(c => c.Name == "session");
        Assert.Contains("id", sessionCmd.Arguments.Select(a => a.Name));
        Assert.Contains("--output", sessionCmd.Options.Select(o => o.Name));
        Assert.Contains("--output", sessionCmd.Options.Select(o => o.Name));
    }

    [Fact]
    public void ExportCommand_ConfigSubcommand_ShouldHaveOutputOpt()
    {
        var cmd = new ExportCommand().Build();
        var configCmd = cmd.Subcommands.First(c => c.Name == "config");
        Assert.Contains("--output", configCmd.Options.Select(o => o.Name));
    }
}
