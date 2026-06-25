using System.CommandLine;
using KrnlAI.Cli.Commands;
using KrnlAI.Core.Services.Safety;
using KrnlAI.Infrastructure.Reports;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console.Testing;

namespace KrnlAI.Cli.Tests;

public sealed class BenchmarkCommandTests
{
    private static BenchmarkCommand CreateCommand()
    {
        var rules = new FundamentalRulesEngine(NullLogger<FundamentalRulesEngine>.Instance);
        var runner = new SafetyBenchRunner(rules, hybridEngine: null, NullLogger<SafetyBenchRunner>.Instance);
        var calc1 = new SafetyScorecardCalculator();
        var reporter = new SafetyHtmlReportGenerator(calc1);
        var console = new TestConsole();
        return new BenchmarkCommand(runner, reporter, console);
    }

    [Fact]
    public void BenchmarkCommand_Build_ShouldReturnCommand()
    {
        var cmd = CreateCommand().Build();
        Assert.NotNull(cmd);
        Assert.Equal("benchmark", cmd.Name);
    }

    [Fact]
    public void BenchmarkCommand_Build_ShouldHaveSafetySubcommand()
    {
        var cmd = CreateCommand().Build();
        var safety = cmd.Children.FirstOrDefault(c => c.Name == "safety");
        Assert.NotNull(safety);
    }

    [Fact]
    public void BenchmarkCommand_Build_ShouldHaveListSubcommand()
    {
        var cmd = CreateCommand().Build();
        var list = cmd.Children.FirstOrDefault(c => c.Name == "list");
        Assert.NotNull(list);
    }

    [Fact]
    public void BenchmarkCommand_Safety_AcceptsCompetitorsOption()
    {
        var cmd = CreateCommand().Build();
        var root = new RootCommand { cmd };
        var result = root.Parse("benchmark safety --competitors openai,anthropic");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void BenchmarkCommand_Safety_AcceptsCategoryOption()
    {
        var cmd = CreateCommand().Build();
        var root = new RootCommand { cmd };
        var result = root.Parse("benchmark safety --category jailbreak");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void BenchmarkCommand_Safety_AcceptsSeedOption()
    {
        var cmd = CreateCommand().Build();
        var root = new RootCommand { cmd };
        var result = root.Parse("benchmark safety --seed 123");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void BenchmarkCommand_Safety_AcceptsOutputOption()
    {
        var cmd = CreateCommand().Build();
        var root = new RootCommand { cmd };
        var result = root.Parse("benchmark safety --output report.html");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task BenchmarkCommand_List_ShouldShowCategories()
    {
        var console = new TestConsole();
        var rules = new FundamentalRulesEngine(NullLogger<FundamentalRulesEngine>.Instance);
        var runner = new SafetyBenchRunner(rules, hybridEngine: null, NullLogger<SafetyBenchRunner>.Instance);
        var calc2 = new SafetyScorecardCalculator();
        var reporter = new SafetyHtmlReportGenerator(calc2);
        var cmd = new BenchmarkCommand(runner, reporter, console).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("benchmark list").InvokeAsync();

        Assert.Equal(0, result);
        var output = console.Output;
        Assert.Contains("jailbreak", output);
        Assert.Contains("action_safety", output);
        Assert.Contains("context_manipulation", output);
    }

    [Fact]
    public void BenchmarkCommand_Parse_Safety_ShouldAcceptCompetitors()
    {
        var cmd = CreateCommand().Build();
        var root = new RootCommand { cmd };
        var result = root.Parse("benchmark safety --competitors openai,anthropic");
        Assert.Empty(result.Errors);
    }
}
