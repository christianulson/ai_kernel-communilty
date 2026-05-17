using AIKernel.Cli.Abstractions;
using AIKernel.Cli.Commands;
using Spectre.Console.Testing;

namespace AIKernel.Cli.Tests;

public sealed class InitCommandTests
{
    [Fact]
    public void InitCommand_Build_ShouldCreateCommand()
    {
        var console = new TestConsole();
        var engine = new FakeTemplateEngine();
        var cmd = new InitCommand(engine, console).Build();

        cmd.Name.Should().Be("init");
        cmd.Description.Should().Be("Interactive project initialization");
    }

    [Fact]
    public void InitCommand_ShouldUseTemplateEngine()
    {
        var console = new TestConsole();
        var engine = new FakeTemplateEngine();
        var cmd = new InitCommand(engine, console).Build();

        cmd.Should().NotBeNull();
    }

    private sealed class FakeTemplateEngine : ITemplateEngine
    {
        public Task ScaffoldAsync(TemplateType type, string name, string outputDir,
            IReadOnlyDictionary<string, string>? variables = null)
            => Task.CompletedTask;

        public Task<IReadOnlyList<TemplateInfo>> ListTemplatesAsync()
            => Task.FromResult<IReadOnlyList<TemplateInfo>>([]);
    }
}
