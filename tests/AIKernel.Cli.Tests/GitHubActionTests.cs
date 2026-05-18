namespace KrnlAI.Cli.Tests;

public sealed class GitHubActionTests
{
    private readonly string _actionPath;

    public GitHubActionTests()
    {
        _actionPath = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            ".github", "actions", "krnlai", "action.yml");
    }

    [Fact]
    public void GitHubAction_ActionFile_ShouldExist()
    {
        var path = Path.GetFullPath(_actionPath);
        Assert.True(File.Exists(path), $"Action file not found: {path}");
    }

    [Fact]
    public void GitHubAction_ActionFile_ShouldContainRequiredFields()
    {
        var path = Path.GetFullPath(_actionPath);
        var content = File.ReadAllText(path);
        Assert.Contains("name:", content);
        Assert.Contains("description:", content);
        Assert.Contains("inputs:", content);
        Assert.Contains("prompt", content);
        Assert.Contains("runs:", content);
    }
}
