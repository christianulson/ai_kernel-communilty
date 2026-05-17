using AIKernel.Cli.Commands;

namespace AIKernel.Cli.Tests;

public sealed class UpgradeCommandTests
{
    [Fact]
    public void UpgradeCommand_Build_ShouldReturnCommand()
    {
        var cmd = new UpgradeCommand().Build();
        Assert.NotNull(cmd);
        Assert.Equal("upgrade", cmd.Name);
        Assert.Contains("--check", cmd.Options.Select(o => o.Name));
        Assert.Contains("--version", cmd.Options.Select(o => o.Name));
    }

    [Fact]
    public void UpgradeCommand_GetCurrentVersion_ShouldReturnVersion()
    {
        var method = typeof(UpgradeCommand).GetMethod("GetCurrentVersion",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        var version = method.Invoke(null, []);
        Assert.NotNull(version);
        Assert.IsType<string>(version);
        Assert.NotEmpty((string)version);
    }
}
