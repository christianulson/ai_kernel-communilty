using System.CommandLine;
using KrnlAI.Cli.Commands;
using KrnlAI.Cli.Services;
using KrnlAI.LLMGateway.Core.Abstractions;
using KrnlAI.LLMGateway.Core.Services.Goals;
using KrnlAI.LLMGateway.Core.Services.Governance;
using KrnlAI.Contracts;
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

public sealed class GoalsCommandTests
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
        services.AddSingleton<IGoalStore>(_ =>
        {
            var store = new InMemoryGoalStore();
            store.UpsertAsync(new PersistentGoal(
                "goal-001", null, "Test goal", GoalStatus.Active, 0.5, 0.8,
                DateTimeOffset.UtcNow, null, [], [], new Dictionary<string, string>()),
                CancellationToken.None).GetAwaiter().GetResult();
            return store;
        });
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
    public async Task GoalsCommand_List_ShouldShowActiveGoals()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new GoalsCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("goals list").InvokeAsync();

        result.Should().Be(0);
        var output = console.Output;
        output.Should().Contain("goal-001");
        output.Should().Contain("Test goal");
    }

    [Fact]
    public async Task GoalsCommand_Get_ById_ShouldShowDetails()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new GoalsCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("goals get goal-001").InvokeAsync();

        result.Should().Be(0);
        var output = console.Output;
        output.Should().Contain("GoalId:");
        output.Should().Contain("goal-001");
        output.Should().Contain("Status:");
        output.Should().Contain("Active");
    }

    [Fact]
    public async Task GoalsCommand_Get_WithUnknownId_ShouldReturnError()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new GoalsCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("goals get unknown-id").InvokeAsync();

        result.Should().Be(1);
        console.Output.Should().Contain("not found");
    }
}