using AIKernel.Cli.Services;
using Kernel.Core.Services.Memory;
using Spectre.Console.Testing;

namespace AIKernel.Cli.Tests;

public sealed class ConsoleRendererTests
{
    private static ConsoleRenderer CreateRenderer()
    {
        var console = new TestConsole();
        return new ConsoleRenderer(console);
    }

    [Fact]
    public void RenderTable_WithItems_ShouldRenderTable()
    {
        var renderer = CreateRenderer();
        var items = new[]
        {
            new { Id = "1", Name = "Foo" },
            new { Id = "2", Name = "Bar" },
        };

        renderer.RenderTable(items, "Id", "Name");
        var output = ((TestConsole)renderer.Console).Output;
        output.Should().Contain("Id");
        output.Should().Contain("Name");
        output.Should().Contain("Foo");
        output.Should().Contain("Bar");
    }

    [Fact]
    public void RenderTable_WithEmptySource_ShouldRenderEmptyMessage()
    {
        var renderer = CreateRenderer();
        var items = Array.Empty<object>();

        renderer.RenderTable(items, "Header");
        var output = ((TestConsole)renderer.Console).Output;
        output.Should().Contain("No data");
    }

    [Fact]
    public void RenderDetail_ShouldShowAllProperties()
    {
        var renderer = CreateRenderer();
        var item = new { Prop1 = "value1", Prop2 = "value2" };

        renderer.RenderDetail(item);
        var output = ((TestConsole)renderer.Console).Output;
        output.Should().Contain("Prop1");
        output.Should().Contain("value1");
        output.Should().Contain("Prop2");
        output.Should().Contain("value2");
    }

    [Fact]
    public void RenderStatus_WithCognitiveState_ShouldShowBasicInfo()
    {
        var renderer = CreateRenderer();
        var state = new CognitiveState(0.45, 0.12, 0.30, 0.85, CognitiveStateFlags.None);

        renderer.RenderStatus(state, "Running", "Deep", 3, 2, 5, "1.2s ago", "124ms");
        var output = ((TestConsole)renderer.Console).Output;
        output.Should().Contain("Running");
        output.Should().Contain("Deep");
        output.Should().Contain("Health Score");
        output.Should().Contain("Active Goals");
    }

    [Fact]
    public void RenderHealth_WithMixedStatuses_ShouldShowAllModules()
    {
        var renderer = CreateRenderer();
        var modules = new[]
        {
            new { Name = "SensoryPipeline", Status = "ok", Latency = "12ms" },
            new { Name = "SafetyChecker", Status = "degraded", Latency = "120ms" },
            new { Name = "ExternalLLM", Status = "unavailable", Latency = "0ms" },
        };

        renderer.RenderHealth(modules, "DEGRADED (1 warning, 1 failure)");
        var output = ((TestConsole)renderer.Console).Output;
        output.Should().Contain("SensoryPipeline");
        output.Should().Contain("SafetyChecker");
        output.Should().Contain("DEGRADED");
    }
}
