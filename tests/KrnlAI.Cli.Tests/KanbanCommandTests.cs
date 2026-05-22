using System.CommandLine;
using System.Globalization;
using KrnlAI.Anticipation;
using KrnlAI.Cli.Commands;
using KrnlAI.Cli.Services;
using KrnlAI.Contracts;
using KrnlAI.Core.Abstractions;
using KrnlAI.Core.Services.Memory;
using KrnlAI.Core.Services.Safety;
using KrnlAI.Executive;
using KrnlAI.LLMGateway.Core.Abstractions;
using KrnlAI.LLMGateway.Core.Services.Goals;
using KrnlAI.LLMGateway.Core.Services.Governance;
using KrnlAI.Memory;
using KrnlAI.Snapshot;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Testing;

namespace KrnlAI.Cli.Tests;

public sealed class KanbanCommandTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 22, 12, 0, 0, TimeSpan.Zero);

    public KanbanCommandTests()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
    }

    private static (KanbanRenderer Renderer, TestConsole Console, CliContext Ctx) Setup()
    {
        var console = new TestConsole();
        var renderer = new KanbanRenderer(console);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMomentStore, InMemoryMomentStore>();
        services.AddSingleton<IMomentClassifierStore, InMemoryMomentClassifierStore>();
        services.AddSingleton<IArchiveStore>(_ => new InMemoryArchiveStore<object>("test-archive"));
        services.AddSingleton<ICognitiveSnapshotService, InMemorySnapshotStore>();
        services.AddSingleton<ICognitiveHomeostasis>(_ => new CognitiveHomeostasisService());
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
                "g1", null, "Setup infra", "active", 0, 0.8,
                FixedNow, FixedNow.AddDays(3), [], [], new Dictionary<string, string>()),
                CancellationToken.None).GetAwaiter().GetResult();
            store.UpsertAsync(new PersistentGoal(
                "g2", null, "Optimize cache", "active", 0.5, 0.6,
                FixedNow, FixedNow.AddDays(7), [], [], new Dictionary<string, string>()),
                CancellationToken.None).GetAwaiter().GetResult();
            store.UpsertAsync(new PersistentGoal(
                "g3", null, "Migration", "blocked", 0.3, 0.7,
                FixedNow, FixedNow.AddDays(5), [], [], new Dictionary<string, string>()),
                CancellationToken.None).GetAwaiter().GetResult();
            store.UpsertAsync(new PersistentGoal(
                "g4", null, "Deploy v2", "completed", 1.0, 0.9,
                FixedNow.AddDays(-2), null, [], [], new Dictionary<string, string>()),
                CancellationToken.None).GetAwaiter().GetResult();
            store.UpsertAsync(new PersistentGoal(
                "g5", null, "Experiment X", "failed", 0.4, 0.3,
                FixedNow.AddDays(-5), null, [], [], new Dictionary<string, string>()),
                CancellationToken.None).GetAwaiter().GetResult();
            return store;
        });
        services.AddSingleton<IKanbanService>(sp =>
            new KrnlAI.LLMGateway.Core.Services.Goals.KanbanService(
                sp.GetRequiredService<IGoalStore>()));
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
    public async Task KanbanService_Direct_ShouldReturnData()
    {
        var store = new InMemoryGoalStore();
        await store.UpsertAsync(new PersistentGoal(
            "g1", null, "Test", "active", 0, 0.8,
            FixedNow, null, [], [], new Dictionary<string, string>()),
            CancellationToken.None);

        var service = new KrnlAI.LLMGateway.Core.Services.Goals.KanbanService(store);
        var result = await service.GetKanbanAsync(ct: CancellationToken.None);

        result.Should().NotBeNull();
        result.Metadata.TotalGoals.Should().Be(1);
        result.Columns.Should().HaveCount(1);
    }

    [Fact]
    public async Task KanbanCommand_ThroughCli_ShouldReturnZero()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new KanbanCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("kanban").InvokeAsync();

        var output = console.Output;
        result.Should().Be(0, $"Console output: {output}");
    }

    [Fact]
    public async Task KanbanCommand_ShouldRenderBoardWithColumns()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new KanbanCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("kanban").InvokeAsync();

        result.Should().Be(0);
        var output = console.Output;
        output.Should().Contain("Backlog");
        output.Should().Contain("Blocked");
        output.Should().Contain("Failed");
        output.Should().Contain("Total: 5");
    }

    [Fact]
    public async Task KanbanCommand_WithDaysOption_ShouldShowDays()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new KanbanCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("kanban --days 3").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("Days back: 3");
    }

    [Fact]
    public async Task KanbanCommand_WithMinPriority_ShouldShowInFooter()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new KanbanCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("kanban --min-priority 0.5").InvokeAsync();
        result.Should().Be(0);
        console.Output.Should().Contain("Min priority: 0.5");
    }

    [Fact]
    public async Task KanbanCommand_WithSearch_ShouldFilter()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new KanbanCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("kanban --search infra").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("Total: 1");
        console.Output.Should().Contain("infra");
    }

    [Fact]
    public async Task KanbanCommand_EmptyData_ShouldShowNoGoals()
    {
        var console = new TestConsole();
        var renderer = new KanbanRenderer(console);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMomentStore, InMemoryMomentStore>();
        services.AddSingleton<IMomentClassifierStore, InMemoryMomentClassifierStore>();
        services.AddSingleton<IArchiveStore>(_ => new InMemoryArchiveStore<object>("test-archive"));
        services.AddSingleton<ICognitiveSnapshotService, InMemorySnapshotStore>();
        services.AddSingleton<ICognitiveHomeostasis>(_ => new CognitiveHomeostasisService());
        services.AddSingleton<IExecutiveController, ExecutiveController>();
        services.AddSingleton<IExecutiveStageBuilder, ExecutiveStageBuilder>();
        services.AddSingleton<IExecutiveModeSelector, ExecutiveModeSelector>();
        services.AddSingleton<IAnticipationStore, InMemoryAnticipationStore>();
        services.AddSingleton<IProspectiveMemoryStore, InMemoryProspectiveMemoryStore>();
        services.AddSingleton<IProspectiveMemoryService>(sp =>
            new ProspectiveMemoryService(sp.GetRequiredService<IProspectiveMemoryStore>()));
        services.AddSingleton<IAnticipationService>(sp =>
            new AnticipationService(Enumerable.Empty<IProjectionSource>(), sp.GetRequiredService<IAnticipationStore>()));
        services.AddSingleton<IGoalStore>(_ => new InMemoryGoalStore());
        services.AddSingleton<IKanbanService>(sp =>
            new KrnlAI.LLMGateway.Core.Services.Goals.KanbanService(
                sp.GetRequiredService<IGoalStore>()));
        services.AddSingleton<ISafetyCaseStore, InMemorySafetyCaseStore>();
        services.AddSingleton<FundamentalRulesEngine>();
        services.AddSingleton<KrnlAI.Core.Abstractions.Mcp.IMcpServerRegistry>(new KrnlAI.Infrastructure.Mcp.McpServerRegistry());
        services.AddSingleton<KrnlAI.Core.Services.ModelRegistry.IModelRegistry>(new KrnlAI.Infrastructure.InMemory.InMemoryModelRegistry());
        services.AddSingleton<KrnlAI.Core.Services.ExperimentTracking.IExperimentTracker>(new KrnlAI.Infrastructure.InMemory.InMemoryExperimentTracker());
        var sp = services.BuildServiceProvider();
        var ctx = new CliContext(sp);

        var cmd = new KanbanCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("kanban").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("No goals found");
    }

    [Fact]
    public async Task KanbanCommand_WithDomainOption_ShouldShowInFooter()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new KanbanCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("kanban --domain testing").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("Domain: testing");
    }
}
