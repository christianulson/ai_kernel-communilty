using System.CommandLine;
using KrnlAI.Cli.Commands;
using KrnlAI.Cli.Services;
using KrnlAI.LLMGateway.Core.Abstractions;
using KrnlAI.LLMGateway.Core.Services.Goals;
using KrnlAI.LLMGateway.Core.Services.Governance;
using Kernel.Core.Abstractions;
using Kernel.Core.Services.Memory;
using Kernel.Core.Services.Safety;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Testing;
using Kernel.Anticipation;
using Kernel.Executive;
using Kernel.Snapshot;
using Kernel.Memory;

namespace KrnlAI.Cli.Tests;

public sealed class MomentsCommandTests
{
    private static (ConsoleRenderer Renderer, TestConsole Console, CliContext Ctx) Setup()
    {
        var console = new TestConsole();
        var renderer = new ConsoleRenderer(console);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMomentStore>(_ =>
        {
            var store = new InMemoryMomentStore();
            var now = DateTimeOffset.UtcNow;
            store.UpsertAsync(
                new MomentSnapshot("mom-001", 1, now.AddMinutes(-10), now.AddMinutes(-9),
                    null, null, 0.72, 0.85, -0.32, [], [], [], [], new Dictionary<string, string>()),
                CancellationToken.None).GetAwaiter().GetResult();
            store.UpsertAsync(
                new MomentSnapshot("mom-002", 2, now.AddMinutes(-8), now.AddMinutes(-7),
                    null, null, 0.32, 0.40, 0.10, [], [], [], [], new Dictionary<string, string>()),
                CancellationToken.None).GetAwaiter().GetResult();
            return store;
        });
        services.AddSingleton<IMomentClassifierStore>(_ =>
        {
            var store = new InMemoryMomentClassifierStore();
            store.StoreAsync(
                new MomentClassification("mom-001", MomentCategory.Anomaly, 0.92,
                    MomentImportance.Zero, MomentNarrativeRole.None, [], new Dictionary<string, string>()),
                CancellationToken.None).GetAwaiter().GetResult();
            return store;
        });
        services.AddSingleton<IArchiveStore>(_ => new InMemoryArchiveStore<object>("test-archive"));
        services.AddSingleton<ICognitiveSnapshotService, InMemorySnapshotStore>();
        services.AddSingleton<ICognitiveHomeostasis>(_ => new CognitiveHomeostasisService());
        services.AddSingleton<IExecutiveController, ExecutiveController>();
        services.AddSingleton<IAnticipationStore, InMemoryAnticipationStore>();
        services.AddSingleton<IProspectiveMemoryStore, InMemoryProspectiveMemoryStore>();
        services.AddSingleton<IProspectiveMemoryService>(sp =>
            new ProspectiveMemoryService(sp.GetRequiredService<IProspectiveMemoryStore>()));
        services.AddSingleton<IAnticipationService>(sp =>
            new AnticipationService(
                Enumerable.Empty<Kernel.Core.Abstractions.IProjectionSource>(),
                sp.GetRequiredService<IAnticipationStore>()));
        services.AddSingleton<IGoalStore, InMemoryGoalStore>();
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
    public async Task MomentsCommand_ListRecent_ShouldCallMomentStore()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new MomentsCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("moments recent --take 5").InvokeAsync();

        result.Should().Be(0);
        var output = console.Output;
        output.Should().Contain("mom-001");
        output.Should().Contain("mom-002");
    }

    [Fact]
    public async Task MomentsCommand_GetById_ShouldReturnDetail()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new MomentsCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("moments get mom-001").InvokeAsync();

        result.Should().Be(0);
        var output = console.Output;
        output.Should().Contain("MomentId:");
        output.Should().Contain("mom-001");
        output.Should().Contain("Cognitive Load:");
        output.Should().Contain("0,72");
    }
}
