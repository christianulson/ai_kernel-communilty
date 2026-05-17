using System.CommandLine;
using AIKernel.Cli.Commands;
using Spectre.Console.Testing;

namespace AIKernel.Cli.Tests;

public sealed class ConfigCommandTests
{
    [Fact]
    public void ConfigCommand_Build_ShouldCreateCommand()
    {
        var console = new TestConsole();
        var cmd = new ConfigCommand(console).Build();

        cmd.Name.Should().Be("config");
        cmd.Description.Should().Be("Manage declarative YAML configuration");
        cmd.Children.Should().HaveCount(3);
    }

    [Fact]
    public async Task ConfigCommand_Show_ShouldPrintMessage()
    {
        var console = new TestConsole();
        var cmd = new ConfigCommand(console).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("config show").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("Current configuration");
    }

    [Fact]
    public async Task ConfigCommand_Export_ShouldProduceOutput()
    {
        var console = new TestConsole();
        var cmd = new ConfigCommand(console).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("config export test-config --format yaml").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("Exported configuration");
    }

    [Fact]
    public async Task ConfigCommand_Validate_WithInvalidPath_ShouldFail()
    {
        var console = new TestConsole();
        var cmd = new ConfigCommand(console).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("config validate nonexistent.yaml").InvokeAsync();

        result.Should().Be(1);
        console.Output.Should().Contain("File not found");
    }
}
