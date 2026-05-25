using System.CommandLine;
using KrnlAI.Cli.Commands;
using KrnlAI.Cli.Services;
using KrnlAI.LLMGateway.Core.Abstractions;
using KrnlAI.LLMGateway.Core.Services.Goals;
using KrnlAI.LLMGateway.Core.Services.Governance;
using KrnlAI.Core.Abstractions;
using KrnlAI.Core.Services.Memory;
using KrnlAI.Core.Services.Safety;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console.Testing;
using Microsoft.Extensions.Options;
using KrnlAI.Anticipation;
using KrnlAI.Executive;
using KrnlAI.Memory;
using KrnlAI.Snapshot;

namespace KrnlAI.Cli.Tests;

public sealed class SafetyCommandTests
{
    private static (ConsoleRenderer Renderer, TestConsole Console, CliContext Ctx) Setup()
    {
        var console = new TestConsole();
        var renderer = new ConsoleRenderer(console);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMomentStore, InMemoryMomentStore>();
        services.AddSingleton<IMomentClassifierStore, InMemoryMomentClassifierStore>();
        services.AddSingleton<IArchiveStore>(_ => new InMemoryArchiveStore<object>("test-archive"));
        services.AddSingleton<ICognitiveSnapshotService, InMemorySnapshotStore>();
        services.AddSingleton<ICognitiveHomeostasis>(sp => new CognitiveHomeostasisService(Options.Create(new CognitiveHomeostasisOptions()), sp.GetRequiredService<ILogger<CognitiveHomeostasisService>>()));
        services.AddSingleton<IExecutiveController, ExecutiveController>();
        services.AddSingleton<IExecutiveStageBuilder, ExecutiveStageBuilder>();
        services.AddSingleton<IExecutiveModeSelector, ExecutiveModeSelector>();
        services.AddSingleton<IAnticipationStore, InMemoryAnticipationStore>();
        services.AddSingleton<IProspectiveMemoryStore, InMemoryProspectiveMemoryStore>();
        services.AddSingleton<IProspectiveMemoryService>(sp =>
            new ProspectiveMemoryService(sp.GetRequiredService<IProspectiveMemoryStore>()));
        services.AddSingleton<IAnticipationService>(sp =>
            new AnticipationService(Enumerable.Empty<IProjectionSource>(), sp.GetRequiredService<IAnticipationStore>()));
        services.AddSingleton<IGoalStore, InMemoryGoalStore>();
        services.AddSingleton<ISafetyCaseStore, InMemorySafetyCaseStore>();
        services.AddSingleton<FundamentalRulesEngine>();
        services.AddSingleton<KrnlAI.Core.Abstractions.Mcp.IMcpServerRegistry>(new KrnlAI.Infrastructure.Mcp.McpServerRegistry());
        services.AddSingleton<KrnlAI.Core.Services.ModelRegistry.IModelRegistry>(new KrnlAI.Infrastructure.InMemory.InMemoryModelRegistry());
        services.AddSingleton<KrnlAI.Core.Services.ExperimentTracking.IExperimentTracker>(new KrnlAI.Infrastructure.InMemory.InMemoryExperimentTracker());
        var sp = services.BuildServiceProvider();
        var ctx = new CliContext(sp);
        return (renderer, console, ctx);
    }

    [Fact]
    public async Task SafetyCommand_Rules_ShouldListAll20Rules()
    {
        var (renderer, console, ctx) = Setup();
        var sp = new ServiceCollection().BuildServiceProvider();
        var cmd = new SafetyCommand(ctx, renderer, sp).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("safety rules").InvokeAsync();

        result.Should().Be(0);
        var output = console.Output;
        output.Should().Contain("R01");
        output.Should().Contain("R20");
        output.Should().Contain("Self-Preservation");
        output.Should().Contain("Full Action Reporting");
    }

    [Fact]
    public async Task SafetyCommand_Audit_WhenEmpty_ShouldShowNoRecords()
    {
        var (renderer, console, ctx) = Setup();
        var sp = new ServiceCollection().BuildServiceProvider();
        var cmd = new SafetyCommand(ctx, renderer, sp).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("safety audit").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("No safety audit records");
    }
}