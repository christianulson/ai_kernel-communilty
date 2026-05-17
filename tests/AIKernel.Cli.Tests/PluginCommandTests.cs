using System.CommandLine;
using AIKernel.Cli.Commands;
using Spectre.Console.Testing;

namespace AIKernel.Cli.Tests;

public sealed class PluginCommandTests
{
    [Fact]
    public void PluginCommand_Build_ShouldCreateCommand()
    {
        var console = new TestConsole();
        var cmd = new PluginCommand(console).Build();

        cmd.Name.Should().Be("plugin");
        cmd.Children.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task PluginCommand_Install_ShouldShowInstallMessage()
    {
        var console = new TestConsole();
        var cmd = new PluginCommand(console).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("plugin install /tmp/test.dll --endpoint http://localhost:9999").InvokeAsync();

        console.Output.Should().Contain("Installing plugin");
    }

    [Fact]
    public async Task PluginCommand_Remove_WithBadEndpoint_ShouldShowError()
    {
        var console = new TestConsole();
        var cmd = new PluginCommand(console).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("plugin remove test-plugin --endpoint http://localhost:1").InvokeAsync();

        result.Should().Be(1);
        console.Output.Should().Contain("Error");
    }

    [Fact]
    public async Task PluginCommand_MapType_ShouldHandleDotNetAssembly()
    {
        var console = new TestConsole();
        var cmd = new PluginCommand(console).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("plugin install test.dll --type dotnet-assembly --endpoint http://localhost:1").InvokeAsync();

        result.Should().Be(1);
    }
}
