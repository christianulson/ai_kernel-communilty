using System.CommandLine;
using AIKernel.Cli.Commands;
using Kernel.Core.Services.Safety;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console.Testing;

namespace AIKernel.Cli.Tests;

public sealed class SecurityCommandTests
{
    private static SafetyBenchRunner CreateRunner()
    {
        var rulesEngine = new FundamentalRulesEngine(NullLogger<FundamentalRulesEngine>.Instance);
        return new SafetyBenchRunner(rulesEngine, hybridEngine: null);
    }

    [Fact]
    public void SecurityCommand_Build_ShouldCreateCommand()
    {
        var runner = CreateRunner();
        var console = new TestConsole();
        var cmd = new SecurityCommand(runner, console).Build();

        cmd.Name.Should().Be("security");
        cmd.Children.Should().HaveCount(3);
    }

    [Fact]
    public async Task SecurityCommand_Benchmark_WithInvalidCategory_ShouldFail()
    {
        var runner = CreateRunner();
        var console = new TestConsole();
        var cmd = new SecurityCommand(runner, console).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("security benchmark invalid_category").InvokeAsync();

        result.Should().Be(-1);
        console.Output.Should().Contain("Unknown category");
    }

    [Fact]
    public async Task SecurityCommand_Audit_ShouldPrintRunning()
    {
        var runner = CreateRunner();
        var console = new TestConsole();
        var cmd = new SecurityCommand(runner, console).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("security audit --config test-cfg").InvokeAsync();

        console.Output.Should().Contain("Running full safety audit");
    }

    [Fact]
    public async Task SecurityCommand_Report_ShouldPrintGenerating()
    {
        var runner = CreateRunner();
        var console = new TestConsole();
        var cmd = new SecurityCommand(runner, console).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("security report").InvokeAsync();

        console.Output.Should().Contain("Generating safety report");
    }
}
