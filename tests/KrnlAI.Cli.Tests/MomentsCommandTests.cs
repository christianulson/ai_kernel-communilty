using AutoFixture;
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
using KrnlAI.Snapshot;
using KrnlAI.Memory;
using TestHelpers;

namespace KrnlAI.Cli.Tests;

public sealed class MomentsCommandTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();
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
                Fixture.Build<MomentClassification>()
                    .With(x => x.MomentId, "mom-001")
                    .With(x => x.Category, MomentCategory.Anomaly)
                    .With(x => x.Confidence, 0.92)
                    .With(x => x.Importance, MomentImportance.Zero)
                    .With(x => x.NarrativeRole, MomentNarrativeRole.None)
                    .With(x => x.Tags, (IReadOnlyList<string>)[])
                    .With(x => x.Metadata, new Dictionary<string, string>())
                    .Create(),
                CancellationToken.None).GetAwaiter().GetResult();
            return store;
        });
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
            new AnticipationService(
                [],
                sp.GetRequiredService<IAnticipationStore>()));
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
