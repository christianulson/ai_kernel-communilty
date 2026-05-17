using System.CommandLine;
using AIKernel.Cli.Commands;
using Spectre.Console.Testing;

namespace AIKernel.Cli.Tests;

public sealed class ServeCommandTests
{
    [Fact]
    public void ServeCommand_Build_ShouldCreateCommand()
    {
        var cmd = new ServeCommand().Build();

        cmd.Name.Should().Be("serve");
        cmd.Description.Should().Be("Start headless HTTP server");
    }

    [Fact]
    public void ServeCommand_ShouldHavePortOption()
    {
        var cmd = new ServeCommand().Build();

        var portOpt = cmd.Children.FirstOrDefault(c => c is Option<int> opt && opt.Name == "port");
        portOpt.Should().NotBeNull();
    }
}
