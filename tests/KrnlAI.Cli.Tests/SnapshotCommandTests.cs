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
using Spectre.Console.Testing;
using KrnlAI.Anticipation;
using KrnlAI.Executive;
using KrnlAI.Memory;
using KrnlAI.Snapshot;

namespace KrnlAI.Cli.Tests;

public sealed class SnapshotCommandTests
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
    public async Task SnapshotCommand_List_WhenEmpty_ShouldShowNoSnapshots()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new SnapshotCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("snapshot list").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("No snapshots");
    }

    [Fact]
    public async Task SnapshotCommand_Create_ShouldCreateAndShowId()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new SnapshotCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("snapshot create --label test-snap --reason unit-test").InvokeAsync();

        result.Should().Be(0);
        var output = console.Output;
        output.Should().Contain("Snapshot created");
        output.Should().Contain("test-snap");
    }

    [Fact]
    public async Task SnapshotCommand_List_AfterCreate_ShouldShowSnapshot()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new SnapshotCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        await root.Parse("snapshot create --label list-test").InvokeAsync();
            var result = await root.Parse("snapshot list").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("list-test");
    }

    [Fact]
    public async Task SnapshotCommand_Delete_ShouldRemoveSnapshot()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new SnapshotCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        await root.Parse("snapshot create --label del-test").InvokeAsync();
            var snapshots = await ctx.SnapshotService.ListSnapshotsAsync(null, CancellationToken.None);
            var id = snapshots[0].Id.Value;

            var result = await root.Parse($"snapshot delete {id}").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("Snapshot deleted");
    }

    [Fact]
    public async Task SnapshotCommand_Restore_ShouldSucceed()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new SnapshotCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        await root.Parse("snapshot create --label restore-test").InvokeAsync();
            var snapshots = await ctx.SnapshotService.ListSnapshotsAsync(null, CancellationToken.None);
            var id = snapshots[0].Id.Value;

            var result = await root.Parse($"snapshot restore {id}").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("Snapshot restored");
    }

    [Fact]
    public async Task SnapshotCommand_Create_WithInvalidScope_ShouldReturnError()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new SnapshotCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("snapshot create --scope InvalidScope").InvokeAsync();

        result.Should().Be(1);
        console.Output.Should().Contain("Invalid scope");
    }
}