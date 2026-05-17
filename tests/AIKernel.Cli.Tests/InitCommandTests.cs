using System.CommandLine;
using AIKernel.Cli.Abstractions;
using AIKernel.Cli.Commands;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console.Testing;

namespace AIKernel.Cli.Tests;

public sealed class InitCommandTests
{
    [Fact]
    public async Task InitCommand_Interactive_ShouldScaffoldAgent()
    {
        var console = new TestConsole();
        var engine = new FakeTemplateEngine();
        var cmd = new InitCommand(engine, console).Build();
        var root = new RootCommand { cmd };

        console.Input.PushTextWithEnter("test-agent");
        console.Input.PushTextWithEnter("strict");
        console.Input.PushTextWithEnter("y");
        console.Input.PushTextWithEnter("ollama");

        var result = await root.Parse("init").InvokeAsync();

        result.Should().Be(0);
        engine.LastType.Should().Be(TemplateType.Agent);
        engine.LastName.Should().Be("test-agent");
    }

    [Fact]
    public async Task InitCommand_WithExistingDir_ShouldFail()
    {
        var console = new TestConsole();
        var engine = new FakeTemplateEngine { ThrowOnExisting = true };
        var cmd = new InitCommand(engine, console).Build();
        var root = new RootCommand { cmd };

        console.Input.PushTextWithEnter("existing-agent");
        console.Input.PushTextWithEnter("strict");
        console.Input.PushTextWithEnter("y");
        console.Input.PushTextWithEnter("ollama");

        var result = await root.Parse("init").InvokeAsync();

        result.Should().Be(-1);
    }

    private sealed class FakeTemplateEngine : ITemplateEngine
    {
        public bool ThrowOnExisting { get; set; }
        public TemplateType? LastType { get; private set; }
        public string? LastName { get; private set; }

        public Task ScaffoldAsync(TemplateType type, string name, string outputDir, IReadOnlyDictionary<string, string>? variables = null)
        {
            LastType = type;
            LastName = name;
            if (ThrowOnExisting && Directory.Exists(outputDir))
                throw new DirectoryNotFoundException($"Directory {outputDir} exists");
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<TemplateInfo>> ListTemplatesAsync()
            => Task.FromResult<IReadOnlyList<TemplateInfo>>([]);
    }
}
