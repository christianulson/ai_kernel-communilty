using KrnlAI.Cli.Commands;

namespace KrnlAI.Cli.Tests;

public sealed class InteractiveCommandTests
{
    [Fact]
    public void InteractiveCommand_Build_ShouldExposeLocalAndModelOptions()
    {
        var cmd = new InteractiveCommand().Build();

        cmd.Options.Select(o => o.Name).Should().Contain(["--local", "--model"]);
    }
}
