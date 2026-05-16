using AIKernel.Cli.Commands;
using Spectre.Console.Testing;

namespace AIKernel.Cli.Tests;

public sealed class IntegrationCommandTests
{
    [Fact]
    public void Build_ShouldCreateCommand()
    {
        var console = new TestConsole();
        var cmd = new IntegrationCommand(console).Build();
        cmd.Should().NotBeNull();
        cmd.Name.Should().Be("integration");
    }

    [Fact]
    public async Task Config_OpenAI_ShouldShowVariables()
    {
        try
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", "sk-test-key");
            var console = new TestConsole();
            var cmd = new IntegrationCommand(console).Build();

            var result = await cmd.Parse("config OpenAI").InvokeAsync();

            result.Should().Be(0);
            console.Output.Should().Contain("OPENAI_API_KEY");
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        }
    }

    [Fact]
    public async Task Config_UnknownProvider_ShouldShowGenericKeys()
    {
        var console = new TestConsole();
        var cmd = new IntegrationCommand(console).Build();

        var result = await cmd.Parse("config UnknownProvider").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("UNKNOWNPROVIDER_API_KEY");
    }

    [Fact]
    public async Task Add_ShouldWriteEnvFile()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmpDir);
        try
        {
            var originalDir = Environment.CurrentDirectory;
            Environment.CurrentDirectory = tmpDir;

            var console = new TestConsole();
            var cmd = new IntegrationCommand(console).Build();

            var result = await cmd.Parse("add OpenAI").InvokeAsync();

            result.Should().Be(0);
            var envFile = Path.Combine(tmpDir, ".env");
            File.Exists(envFile).Should().BeTrue();
            var content = File.ReadAllText(envFile);
            content.Should().Contain("OPENAI_API_KEY");

            Environment.CurrentDirectory = originalDir;
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }
}
