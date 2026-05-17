using System.CommandLine;
using AIKernel.Cli.Commands;
using AIKernel.Cli.Services;
using AIKernel.LLMGateway.Core.Abstractions;
using AIKernel.LLMGateway.Core.Services.Goals;
using AIKernel.LLMGateway.Core.Services.Governance;
using Kernel.Contracts;
using Kernel.Core.Abstractions;
using Kernel.Core.Services.Memory;
using Kernel.Core.Services.Safety;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Testing;
using Kernel.Anticipation;
using Kernel.Executive;
using Kernel.Memory;
using Kernel.Snapshot;

namespace AIKernel.Cli.Tests;

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
        services.AddSingleton<ICognitiveHomeostasis>(_ => new CognitiveHomeostasisService());
        services.AddSingleton<IExecutiveController, ExecutiveController>();
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
                "goal-001", null, "Test goal", "active", 0.5, 0.8,
                DateTimeOffset.UtcNow, null, [], [], new Dictionary<string, string>()),
                CancellationToken.None).GetAwaiter().GetResult();
            return store;
        });
        services.AddSingleton<ISafetyCaseStore, InMemorySafetyCaseStore>();
        services.AddSingleton<FundamentalRulesEngine>();
        services.AddSingleton<Kernel.Core.Abstractions.Mcp.IMcpServerRegistry>(new Kernel.Infrastructure.Mcp.McpServerRegistry());
        services.AddSingleton<Kernel.Core.Services.ModelRegistry.IModelRegistry>(new Kernel.Core.Services.ModelRegistry.InMemoryModelRegistry());
        services.AddSingleton<Kernel.Core.Services.ExperimentTracking.IExperimentTracker>(new Kernel.Core.Services.ExperimentTracking.InMemoryExperimentTracker());
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
        output.Should().Contain("active");
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