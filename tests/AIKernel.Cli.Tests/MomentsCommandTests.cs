using System.CommandLine;
using AIKernel.Cli.Commands;
using AIKernel.Cli.Services;
using Kernel.Core.Abstractions;
using Kernel.Core.Services.Anticipation;
using Kernel.Core.Services.Memory;
using Kernel.Core.Services.TemporalDepth;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Testing;

namespace AIKernel.Cli.Tests;

public sealed class MomentsCommandTests
{
    private static (ConsoleRenderer Renderer, TestConsole Console, CliContext Ctx) Setup()
    {
        var console = new TestConsole();
        var renderer = new ConsoleRenderer(console);
        var services = new ServiceCollection();
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
