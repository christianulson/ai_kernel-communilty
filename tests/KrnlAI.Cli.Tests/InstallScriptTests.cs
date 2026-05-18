namespace KrnlAI.Cli.Tests;

public sealed class InstallScriptTests
{
    private readonly string _scriptsDir;

    public InstallScriptTests()
    {
        _scriptsDir = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "scripts");
    }

    [Fact]
    public void InstallScript_InstallSh_ShouldExist()
    {
        var path = Path.GetFullPath(Path.Combine(_scriptsDir, "install.sh"));
        Assert.True(File.Exists(path), $"install.sh not found: {path}");
    }

    [Fact]
    public void InstallScript_InstallSh_ShouldBeValidBash()
    {
        var path = Path.GetFullPath(Path.Combine(_scriptsDir, "install.sh"));
        var content = File.ReadAllText(path);
        Assert.StartsWith("#!/bin/bash", content);
        Assert.Contains("dotnet tool install", content);
        Assert.Contains("krnlai --help", content);
    }

    [Fact]
    public void InstallScript_InstallPs1_ShouldExist()
    {
        var path = Path.GetFullPath(Path.Combine(_scriptsDir, "install.ps1"));
        Assert.True(File.Exists(path), $"install.ps1 not found: {path}");
    }

    [Fact]
    public void InstallScript_InstallPs1_ShouldBeValidPowerShell()
    {
        var path = Path.GetFullPath(Path.Combine(_scriptsDir, "install.ps1"));
        var content = File.ReadAllText(path);
        Assert.Contains("dotnet tool install", content);
        Assert.Contains("KrnlAI.Cli", content);
    }

    [Fact]
    public void InstallScript_WorkflowFiles_ShouldExist()
    {
        var workflowsDir = Path.GetFullPath(
            Path.Combine(_scriptsDir, "..", ".github", "workflows"));

        Assert.True(Directory.Exists(workflowsDir), $"Workflows dir not found: {workflowsDir}");

        var files = Directory.GetFiles(workflowsDir, "*.yml");
        Assert.NotEmpty(files);
        Assert.Contains(files, f => f.Contains("pr-review"));
        Assert.Contains(files, f => f.Contains("vsix-publish"));
    }
}
