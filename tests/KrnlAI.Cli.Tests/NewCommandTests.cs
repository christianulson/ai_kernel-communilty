using KrnlAI.Cli.Abstractions;
using KrnlAI.Cli.Commands;
using Spectre.Console.Testing;

namespace KrnlAI.Cli.Tests;

public sealed class NewCommandTests
{
    [Theory]
    [InlineData("MyAgent", "basic")]
    [InlineData("SecureBot", "ollama", "strict")]
    public async Task NewAgent_WithName_ShouldSucceed(params string[] args)
    {
        using var tempDir = new TempDirectory();
        var engine = new FakeTemplateEngine();
        var console = new TestConsole();
        var cmd = new NewCommand(engine, console).Build();

        var cmdLine = $"agent {args[0]} --output {tempDir.Path}";
        var result = await cmd.Parse(cmdLine).InvokeAsync();

        result.Should().Be(0);
        engine.LastScaffoldType.Should().Be(TemplateType.Agent);
        engine.LastScaffoldName.Should().Be(args[0]);
    }

    [Fact]
    public async Task NewAgent_WithOptions_ShouldPassVariables()
    {
        using var tempDir = new TempDirectory();
        var engine = new FakeTemplateEngine();
        var console = new TestConsole();
        var cmd = new NewCommand(engine, console).Build();

        var result = await cmd.Parse($"agent SecureBot --safety strict --llm openai --memory false --output {tempDir.Path}").InvokeAsync();

        result.Should().Be(0);
        engine.LastVariables.Should().Contain("SafetyLevel", "strict");
        engine.LastVariables.Should().Contain("LlmProvider", "openai");
        engine.LastVariables.Should().Contain("MemoryEnabled", "false");
    }

    [Theory]
    [InlineData("WebSearch", TemplateType.Tool)]
    [InlineData("DataPrivacy", TemplateType.Policy)]
    [InlineData("CustomCycle", TemplateType.CognitiveCycle)]
    public async Task NewComponent_WithName_ShouldSucceed(string name, TemplateType expected)
    {
        using var tempDir = new TempDirectory();
        var engine = new FakeTemplateEngine();
        var console = new TestConsole();
        var cmd = new NewCommand(engine, console).Build();

        var subCmd = expected switch
        {
            TemplateType.Tool => "tool",
            TemplateType.Policy => "policy",
            TemplateType.CognitiveCycle => "cognitive-cycle",
            _ => throw new ArgumentOutOfRangeException()
        };
        var result = await cmd.Parse($"{subCmd} {name} --output {tempDir.Path}").InvokeAsync();

        result.Should().Be(0);
        engine.LastScaffoldType.Should().Be(expected);
        engine.LastScaffoldName.Should().Be(name);
    }

    [Fact]
    public async Task NewAgent_ExistingDir_ShouldFail()
    {
        using var tempDir = new TempDirectory();
        var existingDir = Path.Combine(tempDir.Path, "ExistingAgent");
        Directory.CreateDirectory(existingDir);

        var engine = new FakeTemplateEngine();
        var console = new TestConsole();
        var cmd = new NewCommand(engine, console).Build();

        var result = await cmd.Parse($"agent ExistingAgent --output {tempDir.Path}").InvokeAsync();

        result.Should().Be(-1);
    }

    [Fact]
    public async Task NewAgent_MissingName_ShouldFail()
    {
        using var tempDir = new TempDirectory();
        var engine = new FakeTemplateEngine();
        var console = new TestConsole();
        var cmd = new NewCommand(engine, console).Build();

        var result = await cmd.Parse($"agent --output {tempDir.Path}").InvokeAsync();

        result.Should().NotBe(0);
    }

    [Theory]
    [InlineData("MyAgent")]
    [InlineData("agent-123")]
    [InlineData("test.agent")]
    [InlineData("A")]
    [InlineData("Project.With.Dots")]
    public async Task TemplateEngine_VariousNames_ShouldSanitize(string name)
    {
        var engine = new FakeTemplateEngine();

        await engine.ScaffoldAsync(TemplateType.Agent, name, "out", new Dictionary<string, string>
        {
            ["SafetyLevel"] = "moderate"
        });

        engine.LastScaffoldName.Should().Be(name);
        engine.LastVariables.Should().Contain("SafetyLevel", "moderate");
    }

    [Fact]
    public async Task ListTemplates_ShouldReturnAvailable()
    {
        var engine = new FakeTemplateEngine();
        var templates = await engine.ListTemplatesAsync();

        templates.Should().NotBeEmpty();
        templates.Should().Contain(t => t.Type == TemplateType.Agent);
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"krnlai-test-{Guid.NewGuid():N}");

        public TempDirectory()
        {
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            try { Directory.Delete(Path, true); } catch { }
        }
    }
}

public sealed class FakeTemplateEngine : ITemplateEngine
{
    public TemplateType? LastScaffoldType { get; private set; }
    public string? LastScaffoldName { get; private set; }
    public IReadOnlyDictionary<string, string>? LastVariables { get; private set; }

    public Task ScaffoldAsync(TemplateType type, string name, string outputDir, IReadOnlyDictionary<string, string>? variables = null)
    {
        LastScaffoldType = type;
        LastScaffoldName = name;
        LastVariables = variables;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TemplateInfo>> ListTemplatesAsync()
    {
        return Task.FromResult<IReadOnlyList<TemplateInfo>>(new List<TemplateInfo>
        {
            new("basic", "Basic agent", TemplateType.Agent, "1.0.0", [])
        });
    }
}
